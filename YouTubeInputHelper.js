// ==UserScript==
// @name         YouTube Input Helper
// @namespace    http://tampermonkey.net/
// @version      1.0-optimized
// @description  Persistent input box with hotkey focus, fullscreen-safe, optimized lifecycle.
// @author       James
// @match        https://www.youtube.com/*
// @grant        GM_xmlhttpRequest
// @connect      localhost
// ==/UserScript==
//# sourceURL=YouTubeInputHelper.js

(function() {
    'use strict';

    // --- Constants ---
    const INPUT_BOX_ID = 'ahk-custom-input-box';
    const AHK_SERVER_PORT = 50000;
    const INPUT_PLACEHOLDER = '단축키(Ctrl+Alt+0)';
    const INPUT_MAX_LENGTH = 10;

    const INPUT_BOX_STYLE = {
        position: 'fixed',
        top: '10px',
        left: '330px',
        width: '150px',
        height: '20px',
        padding: '5px',
        fontSize: '12px',
        zIndex: '9999',
        border: '1px solid black',
        backgroundColor: 'white',
        color: 'black',
        transition: 'opacity 0.3s ease',
        opacity: '0'  // 처음엔 숨김
    };

    const FOCUS_HOTKEY = { ctrl: true, alt: true, key: '0' };

    // --- Global State ---
    let inputBox = null;  // 한 번만 생성

    // --- Style Injection ---
    // Visual error effects removed as per user request.

    // --- Fullscreen Detection ---
    function isFullScreen() {
        return document.fullscreenElement ||
               document.webkitFullscreenElement ||
               document.mozFullScreenElement ||
               document.msFullscreenElement;
    }

    // --- AHK Communication ---
    function sendTextToAHK(text) {
        if (!text?.trim()) {
            console.log("Input is empty. Nothing to send.");
            return;
        }

        const encodedText = encodeURIComponent(text.trim());
        const requestUrl = `http://localhost:${AHK_SERVER_PORT}/sendText?text=${encodedText}`;

        console.log(`Sending: ${requestUrl}`);
        GM_xmlhttpRequest({
            method: "GET",
            url: requestUrl,
            onload: (res) => {
                console.log("AHK Response:", res.responseText);
                if (inputBox) {
                    inputBox.value = ''; // Clear input on success
                }
            },
            onerror: () => handleRequestError(text)
        });
    }

    function handleRequestError(originalText) {
        if (!inputBox) return;

        // The original placeholder is constant, so we don't need to save it.
        inputBox.value = "http 서버 없음";

        setTimeout(() => {
            if (inputBox) {
                inputBox.value = originalText;
                inputBox.focus();
            }
        }, 5000);
    }

    // --- Input Box Management ---
    function createInputBox() {
        if (inputBox || isFullScreen()) return;

        inputBox = document.createElement('input');
        inputBox.type = 'text';
        inputBox.id = INPUT_BOX_ID;
        inputBox.placeholder = INPUT_PLACEHOLDER;
        inputBox.maxLength = INPUT_MAX_LENGTH;

        Object.assign(inputBox.style, INPUT_BOX_STYLE);

        // Enter key → send
        inputBox.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                const textToSend = inputBox.value;
                sendTextToAHK(textToSend);
                // Clear on success is now handled in GM_xmlhttpRequest's onload
            }
        });

        document.body.appendChild(inputBox);

        // 부드럽게 나타나기
        requestAnimationFrame(() => {
            if (inputBox) inputBox.style.opacity = '1';
        });
    }

    function hideInputBox() {
        if (inputBox) {
            inputBox.style.opacity = '0';
            inputBox.style.pointerEvents = 'none';
            setTimeout(() => {
                if (inputBox) inputBox.style.display = 'none';
            }, 300);
        }
    }

    function showInputBox() {
        if (!inputBox) {
            createInputBox();
            return;
        }
        inputBox.style.display = 'block';
        inputBox.style.pointerEvents = 'auto';
        requestAnimationFrame(() => {
            inputBox.style.opacity = '1';
        });
    }

    // --- Hotkey Focus ---
    document.addEventListener('keydown', (e) => {
        if (e.ctrlKey === FOCUS_HOTKEY.ctrl &&
            e.altKey === FOCUS_HOTKEY.alt &&
            e.key.toUpperCase() === FOCUS_HOTKEY.key.toUpperCase()) {
            e.preventDefault();
            if (inputBox && !isFullScreen()) {
                inputBox.focus();
            }
        }
    });

    // --- Fullscreen Listener ---
    ['fullscreenchange', 'webkitfullscreenchange', 'mozfullscreenchange', 'msfullscreenchange'].forEach(event => {
        document.addEventListener(event, () => {
            if (isFullScreen()) {
                hideInputBox();
            } else {
                setTimeout(showInputBox, 500);  // 안정성 확보
            }
        });
    });

    // --- YouTube SPA Navigation ---
    document.addEventListener('yt-navigate-finish', () => {
        if (!isFullScreen()) {
            showInputBox();
        }
    });

    // --- Initialization ---
    createInputBox();  // 초기 생성

    // setInterval 완전 삭제! (메모리 누수 방지)
    // setInterval(createOrUpdateInputBox, 1000); ← 삭제됨
})();