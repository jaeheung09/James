; Get one external parameter made up of two intenal parameters and clickdrag with parameters x, y positions
The word FORCE below skips the dialog box and replaces the old instance automatically, which is similar in effect to the Reload command.
#singleinstance force

global cnt := 0
global parm, CurX, CurY

if 0 = 1
{		
	Loop %0%	; 0 -> parameter count
	{
    		parm := %A_Index%
	}

	StringSplit, ParmArray, parm, `,	; spliter ","
	Loop, %ParmArray0%
	{
		cnt++
		tmp := ParmArray%a_index%
		if cnt = 1
			CurX := tmp
		else
			CurY := tmp
		if cnt > 2
			break
	}
	CurX += 0	; Convert string to integer
	CurY += 0
	MouseClick, Left, CurX, CurY, , , D
	MouseClickDrag, L, CurX, CurY, 1370, CurY
}

ExitApp