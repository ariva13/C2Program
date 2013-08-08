ECHO Speaking from zone %1 
set /a A=100+%1
C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\plink -ssh -i C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\pi_key.ppk pi@192.168.113.%A% avconv -i sdpself.sdp -f alsa hw:0,0
EcHO Press any key to exit.
pause>null