#!/bin/sh
#

set -e

echo "🚀 Welcome to the running script for DocIntel"

if ! docker info > /dev/null 2>&1; then
  echo "😥 This script uses docker, and it isn't running."
  echo "Please start docker and try again!"
  exit 1
fi

echo ""
echo "🔍 Asking a few questions"

read -p 'Where to store data (./data by default): ' datafolder
read -p 'Where to store configuration (./config by default): ' conffolder
read -p 'Password for postgres (mypassword by default): ' postgrespw
read -p 'Password for synapse (secret by default): ' synapsepw

if [ -z "$datafolder" ]
then
      datafolder="$(pwd)/data"
fi
mkdir -p $datafolder
mkdir -p $datafolder/files
mkdir -p $datafolder/lock

if [ -z "$conffolder" ]
then
      conffolder="$(pwd)/config"
fi
mkdir -p $conffolder

if [ -z "$postgrespw" ]
then
      postgrespw="mypassword"
fi

if [ -z "$synapsepw" ]
then
      synapsepw="secret"
fi

if [ -z "$docinteladmin" ]
then
      docinteladmin="admin"
fi

if [ -z "$docintelpw" ]
then
      docintelpw="testPassword123!"
fi

echo ""
echo "☁️ Pulling all the Docker images"
docker pull postgres
docker pull rabbitmq
docker pull solr
docker pull vertexproject/synapse-cortex:v2.x.x 

docker pull docintelapp/document-analyzer
docker pull docintelapp/document-indexer
docker pull docintelapp/importer
docker pull docintelapp/newsletter
docker pull docintelapp/scraper
docker pull docintelapp/source-indexer
docker pull docintelapp/tag-indexer
docker pull docintelapp/thumbnailer
docker pull docintelapp/webapp

echo ""
echo "🗄️ Configuring PostgreSQL"
docker run --name docintel-dev-postgresql \
  -e POSTGRES_PASSWORD=$postgrespw \
  -e PGUSER=postgres \
  -v $datafolder/postgres/:/var/lib/postgresql/data \
  -d postgres
echo "Wait for PostgreSQL to be up-and-running"
sleep 15
docker exec docintel-dev-postgresql psql -c 'CREATE EXTENSION IF NOT EXISTS "uuid-ossp";'
docker exec docintel-dev-postgresql psql -c 'CREATE DATABASE "docintel";'
docker stop docintel-dev-postgresql
docker rm docintel-dev-postgresql

echo ""
echo "📚 Configuring SolR"
mkdir -p $datafolder/solr
docker run --name docintel-dev-solr \
  -v $datafolder/solr/:/var/solr \
  -d solr
echo "Wait for SolR to be up-and-running"
sleep 60

if [ $(curl -LI http://localhost:8983/solr/admin/cores\?action\=STATUS\&core\=document -o /dev/null -w '%{http_code}\n' -s) != "200" ]
then
  docker exec -it docintel-dev-solr solr create_core -c document
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/solrconfig-document.xml -o $datafolder/solr/data/document/conf/solrconfig.xml
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/managed-schema-document -o $datafolder/solr/data/document/conf/managed-schema.xml 
fi

if [ $(curl -LI http://localhost:8983/solr/admin/cores\?action\=STATUS\&core\=tag -o /dev/null -w '%{http_code}\n' -s) != "200" ]
then
  docker exec -it docintel-dev-solr solr create_core -c tag
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/solrconfig-tag.xml -o $datafolder/solr/data/tag/conf/solrconfig.xml
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/managed-schema-tag -o $datafolder/solr/data/tag/conf/managed-schema.xml 
fi

if [ $(curl -LI http://localhost:8983/solr/admin/cores\?action\=STATUS\&core\=source -o /dev/null -w '%{http_code}\n' -s) != "200" ]
then
  docker exec -it docintel-dev-solr solr create_core -c source
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/solrconfig-source.xml -o $datafolder/solr/data/source/conf/solrconfig.xml
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/managed-schema-source -o $datafolder/solr/data/source/conf/managed-schema.xml 
fi

if [ $(curl -LI http://localhost:8983/solr/admin/cores\?action\=STATUS\&core\=facet -o /dev/null -w '%{http_code}\n' -s) != "200" ]
then
  docker exec -it docintel-dev-solr solr create_core -c facet
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/solrconfig-facet.xml -o $datafolder/solr/data/facet/conf/solrconfig.xml
  curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/managed-schema-facet -o $datafolder/solr/data/facet/conf/managed-schema.xml 
fi

docker stop docintel-dev-solr
docker rm docintel-dev-solr

echo ""
echo "⚙️ Creating the configuration files"
curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/appsettings.json.example -o $conffolder/appsettings.json
sed -i.bck "s~_POSTGRES_USER_~postgres~g" $conffolder/appsettings.json
sed -i.bck "s~_POSTGRES_PW_~$postgrespw~g" $conffolder/appsettings.json
sed -i.bck "s~_POSTGRES_DB_~docintel~g" $conffolder/appsettings.json
sed -i.bck "s~_POSTGRES_PORT_~5432~g" $conffolder/appsettings.json
sed -i.bck "s~_POSTGRES_HOST_~docintel-dev-postgres~g" $conffolder/appsettings.json
sed -i.bck "s~_SYNAPSE_URL_~tcp://root:$synapsepw@docintel-dev-synapse:27492~g" $conffolder/appsettings.json
sed -i.bck "s~_RABBITMQ_HOST_~docintel-dev-rabbitmq~g" $conffolder/appsettings.json
sed -i.bck "s~_RABBITMQ_VHOST_~/~g" $conffolder/appsettings.json
sed -i.bck "s~_RABBITMQ_USER_~guest~g" $conffolder/appsettings.json
sed -i.bck "s~_RABBITMQ_PW_~guest~g" $conffolder/appsettings.json
sed -i.bck "s~_SOLR_URL_~http://docintel-dev-solr:8983~g" $conffolder/appsettings.json

curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/nlog.config.example -o $conffolder/nlog.config

curl https://raw.githubusercontent.com/docintelapp/DocIntel/main/conf/docker-compose.yml.example -o docker-compose.yml
sed -i.bck "s~_DOCINTEL_CONFIG_~$conffolder~g" docker-compose.yml
sed -i.bck "s~_DOCINTEL_DATA_~$datafolder~g" docker-compose.yml
sed -i.bck "s~_POSTGRES_USER_~postgres~g" docker-compose.yml
sed -i.bck "s~_POSTGRES_HOST_~docintel-dev-postgres~g" docker-compose.yml
sed -i.bck "s~_POSTGRES_PW_~$postgrespw~g" docker-compose.yml
sed -i.bck "s~_POSTGRES_DB_~docintel~g" docker-compose.yml
sed -i.bck "s~_POSTGRES_PORT_~5432~g" docker-compose.yml
sed -i.bck "s~_SYNAPSE_URL_~https://docintel-dev-synapse:27492~g" docker-compose.yml
sed -i.bck "s~_SYNAPSE_USER_~root~g" docker-compose.yml
sed -i.bck "s~_SYNAPSE_PW_~$synapsepw~g" docker-compose.yml
sed -i.bck "s~_RABBITMQ_HOST_~docintel-dev-rabbitmq~g" docker-compose.yml
sed -i.bck "s~_RABBITMQ_VHOST_~/~g" docker-compose.yml
sed -i.bck "s~_RABBITMQ_USER_~guest~g" docker-compose.yml
sed -i.bck "s~_RABBITMQ_PW_~guest~g" docker-compose.yml
sed -i.bck "s~_SOLR_URL_~http://docintel-dev-solr:8983~g" docker-compose.yml


