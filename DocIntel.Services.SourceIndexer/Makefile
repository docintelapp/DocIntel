all : clean restore build publish

clean:
	dotnet clean -maxcpucount:1

restore:
	dotnet restore -maxcpucount:1

build: 
	dotnet build -maxcpucount:1

publish: restore
	dotnet publish -maxcpucount:1 -c Debug -r linux-x64 --no-dependencies --self-contained false 

install:
	mkdir -p $(DESTDIR)/usr/lib/docintel/source-indexer/
	cp -R bin/Debug/net7.0/linux-x64/publish/* $(DESTDIR)/usr/lib/docintel/source-indexer/
	rm -f $(DESTDIR)/usr/lib/docintel/source-indexer/appsettings.json 2> /dev/null 
	rm -f $(DESTDIR)/usr/lib/docintel/source-indexer/nlog.config  2> /dev/null

run:
	dotnet run
