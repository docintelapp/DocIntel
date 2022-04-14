#!/usr/bin/env sh
#

set -e

build () {
	rm -rf ./$2;
	cd ./$1;
	sh ../generate-changelog.sh;
	debuild --no-tgz-check --no-lintian --preserve-envvar=HTTP_PROXY --preserve-envvar=HTTPS_PROXY --preserve-envvar=NO_PROXY --preserve-envvar=DOTNET_CLI_TELEMETRY_OPTOUT --preserve-envvar=PATH -us -uc;
	cd ..;
	mv docintel-*.build docintel-*.buildinfo docintel-*.changes docintel-*.deb docintel-*.dsc ./packages/;
}

# wget -O - 'https://dl.bintray.com/rabbitmq/Keys/rabbitmq-release-signing-key.asc' | sudo apt-key add -
# wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
# sudo dpkg -i packages-microsoft-prod.deb
# sudo apt-get install -y apt-transport-https
# sudo apt-get update

# TODO interactive package installation with setup questions
# e.g. LDAP, server address, file location, etc.

###############################################################################
# Packages                                                                    #
###############################################################################

echo $#

if [ "$#" -lt 2 ]; then
	rm -rf packages/*
	build DocIntel.Core docintel-core*
	build DocIntel.AdminConsole docintel-cli*
	build DocIntel.Services.DocumentAnalyzer docintel-document-analyzer*
	build DocIntel.Services.DocumentIndexer docintel-document-indexer*
	build DocIntel.Services.SourceIndexer docintel-source-indexer*
	build DocIntel.Services.TagIndexer docintel-tag-indexer*
	build DocIntel.Services.Thumbnailer docintel-thumbnailer*
	build DocIntel.Services.Importer docintel-importer*
	build DocIntel.Services.Scraper docintel-scraper*
	build DocIntel.Services.Newsletters docintel-email-notifier*
	build DocIntel.Services.ContinuousIndexing docintel-continuous-indexing*
	build DocIntel.WebApp docintel-webapp*
else
	build $1 $2
fi


###############################################################################
# Prepare local repository                                                    #
###############################################################################

cd packages
dpkg-scanpackages -m . > Packages
cd ..


