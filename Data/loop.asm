//setup
@R2
M = 0
@i
M=0

(LOOP)
//if > R1 goto END
@i
D=M
@R1
D=D-M
@END
D;JEQ

//R2=R2+R0
@R0
D=M
@R2
M=D+M

//i++
@i
M=M+1

//Gentag
@LOOP
0;JMP

(END)
@END
0;JMP