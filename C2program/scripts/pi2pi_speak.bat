ECHO Speaking from zone %1 to %2
set /a A=100+%1
set /a B=100+%2
plink -ssh -i pi_key.ppk pi@192.168.113.%A% avconv -ac 1 -f alsa -i hw:1,0 -ar 24000 -acodec pcm_s16be -f rtp rtp://192.168.113.%B%:1234
EcHO Press any key to exit.
pause>null