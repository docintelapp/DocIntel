.PHONY: clean

all : clean restore build publish

clean:
	rm -rf bin/
	rm -rf obj/
	dotnet clean -maxcpucount:1

restore:
	dotnet restore -maxcpucount:1

build:
	yarn webpack
	dotnet build -maxcpucount:1

publish: restore
	dotnet publish -maxcpucount:1 -c Debug -r linux-x64 -p:PublishWithAspNetCoreTargetManifest=false  --self-contained false 

install:
	mkdir -p $(DESTDIR)/usr/lib/docintel/webapp
	cp -R bin/Debug/net6.0/linux-x64/publish/* $(DESTDIR)/usr/lib/docintel/webapp/
	rm -f $(DESTDIR)/usr/lib/docintel/webapp/appsettings.json 2> /dev/null 
	rm -f $(DESTDIR)/usr/lib/docintel/webapp/nlog.config  2> /dev/null
	rm -rf $(DESTDIR)/usr/lib/docintel/webapp/wwwroot
	
	mkdir -p $(DESTDIR)/usr/share/docintel/webapp/wwwroot/dist/
	mkdir -p $(DESTDIR)/usr/share/docintel/webapp/wwwroot/dist2/
	mkdir -p $(DESTDIR)/usr/share/docintel/webapp/wwwroot/images/
	cp -R bin/Debug/net6.0/linux-x64/publish/wwwroot/dist/* $(DESTDIR)/usr/share/docintel/webapp/wwwroot/dist/
	cp -R bin/Debug/net6.0/linux-x64/publish/wwwroot/dist2/* $(DESTDIR)/usr/share/docintel/webapp/wwwroot/dist2/
	cp -R bin/Debug/net6.0/linux-x64/publish/wwwroot/images/* $(DESTDIR)/usr/share/docintel/webapp/wwwroot/images/

run:
	dotnet run
