# videotrim
Cut silence audio from begin and end of a video

I wrote this dotnet app for I did not find a easy way to convert and cut allot of screen recording i am doing on my work tab.

it support hardware acclartions of encoding and coding from nvidia, amd, intel (nec, amf, qsv )

create a file call ffmpeg.txt and put your parms in their example
 -vf "crop=1340:900:240:124" 
 
 if you want using nvida nec hardware encoding 
 -vf "crop=1340:900:240:124" -c:v h264_nvenc
  
This software are a basic warper of ffmpeg

how to use 
trim -i infile.mp4 -o outfile.mkv

or if you got a folder you want cut silence audio at start and end of allot videos
trim -id videofolder -od savetovideofolder

if you want using cuda on video filters add
-cuda on 
example 
trim -id videofolder -od savetovideofolder -cuda on

