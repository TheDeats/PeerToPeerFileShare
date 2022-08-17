FROM mono:latest
RUN mkdir /opt/app
RUN mkdir /files
COPY /networkFileShare/networkFileShare/bin/Debug/networkFileShare.exe /opt/app
CMD ["mono", "/opt/app/networkFileShare.exe"]