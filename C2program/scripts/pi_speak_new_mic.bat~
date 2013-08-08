ECHO Speaking from zone %1 
set /a A=100+%1
set /a B=2000+%1
C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\plink -ssh -i C:\Users\Blake\Documents\Programming\CSharp\C2program\C2program\scripts\pi_key.ppk pi@192.168.113.%A% avconv -ac 1 -f alsa -i hw:1,0 -ar 48000 -acodec pcm_s16le -f rtp rtp://192.168.113.100:%B%
EcHO Press any key to exit.
pause>null
