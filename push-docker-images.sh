if [ -z $1 ]; then 
  echo "Usage:";
  echo "./push-docker-images.sh v2.1 v2.1.1";
  exit 1;
fi

if [ -z $2 ]; then 
  echo "Usage:";
  echo "./push-docker-images.sh v2.1 v2.1.1";
  exit 1;
fi

MAJOR=$1
MINOR=$2

echo "📦 Pushing DocIntel.Services.DocumentAnalyzer"
docker push docintelapp/document-analyzer:latest
docker push docintelapp/document-analyzer:$MAJOR
docker push docintelapp/document-analyzer:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.DocumentIndexer"
docker push docintelapp/document-indexer:latest
docker push docintelapp/document-indexer:$MAJOR
docker push docintelapp/document-indexer:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.Importer"
docker push docintelapp/importer:latest
docker push docintelapp/importer:$MAJOR
docker push docintelapp/importer:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.Newsletters"
docker push docintelapp/newsletter:latest
docker push docintelapp/newsletter:$MAJOR
docker push docintelapp/newsletter:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.Scraper"
docker push docintelapp/scraper:latest
docker push docintelapp/scraper:$MAJOR
docker push docintelapp/scraper:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.SourceIndexer"
docker push docintelapp/source-indexer:latest
docker push docintelapp/source-indexer:$MAJOR
docker push docintelapp/source-indexer:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.TagIndexer"
docker push docintelapp/tag-indexer:latest
docker push docintelapp/tag-indexer:$MAJOR
docker push docintelapp/tag-indexer:$MINOR

echo ""
echo "📦 Pushing DocIntel.Services.Thumbnailer"
docker push docintelapp/thumbnailer:latest
docker push docintelapp/thumbnailer:$MAJOR
docker push docintelapp/thumbnailer:$MINOR

echo ""
echo "📦 Pushing DocIntel.WebApp"
docker push docintelapp/webapp:latest
docker push docintelapp/webapp:$MAJOR
docker push docintelapp/webapp:$MINOR