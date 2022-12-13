if [ -z $1 ]; then 
  echo "Usage:";
  echo "./build-docker-images.sh $MAJOR $MINOR";
  exit 1;
fi

if [ -z $2 ]; then 
  echo "Usage:";
  echo "./build-docker-images.sh $MAJOR $MINOR";
  exit 1;
fi

MAJOR=$1
MINOR=$2
COMPONENT=$3

if [ -z $COMPONENT ] || [ $COMPONENT = "document-analyzer" ]; then 
  echo "📦 Building DocIntel.Services.DocumentAnalyzer"
  docker build -t docintelapp/document-analyzer:latest \
    -t docintelapp/document-analyzer:$MAJOR \
    -t docintelapp/document-analyzer:$MINOR \
    -f ./DocIntel.Services.DocumentAnalyzer/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "document-indexer" ]; then 
  echo "📦 Building DocIntel.Services.DocumentIndexer"
  docker build -t docintelapp/document-indexer:latest  \
    -t docintelapp/document-indexer:$MAJOR  \
    -t docintelapp/document-indexer:$MINOR  \
    -f ./DocIntel.Services.DocumentIndexer/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "importer" ]; then 
  echo "📦 Building DocIntel.Services.Importer"
  docker build -t docintelapp/importer:latest  \
    -t docintelapp/importer:$MAJOR  \
    -t docintelapp/importer:$MINOR  \
    -f ./DocIntel.Services.Importer/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "newsletter" ]; then 
  echo "📦 Building DocIntel.Services.Newsletters"
  docker build -t docintelapp/newsletter:latest  \
    -t docintelapp/newsletter:$MAJOR  \
    -t docintelapp/newsletter:$MINOR  \
    -f ./DocIntel.Services.Newsletters/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "scraper" ]; then 
  echo "📦 Building DocIntel.Services.Scraper"
  docker build -t docintelapp/scraper:latest  \
    -t docintelapp/scraper:$MAJOR  \
    -t docintelapp/scraper:$MINOR  \
    -f ./DocIntel.Services.Scraper/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "source-indexer" ]; then 
  echo "📦 Building DocIntel.Services.SourceIndexer"
  docker build -t docintelapp/source-indexer:latest  \
    -t docintelapp/source-indexer:$MAJOR  \
    -t docintelapp/source-indexer:$MINOR  \
    -f ./DocIntel.Services.SourceIndexer/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "tag-indexer" ]; then 
  echo "📦 Building DocIntel.Services.TagIndexer"
  docker build -t docintelapp/tag-indexer:latest  \
    -t docintelapp/tag-indexer:$MAJOR  \
    -t docintelapp/tag-indexer:$MINOR  \
    -f ./DocIntel.Services.TagIndexer/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "thumbnailer" ]; then 
  echo "📦 Building DocIntel.Services.Thumbnailer"
  docker build -t docintelapp/thumbnailer:latest  \
    -t docintelapp/thumbnailer:$MAJOR  \
    -t docintelapp/thumbnailer:$MINOR  \
    -f ./DocIntel.Services.Thumbnailer/Dockerfile .
  echo ""
fi

if [ -z $COMPONENT ] || [ $COMPONENT = "webapp" ]; then 
  echo "📦 Building DocIntel.WebApp"
  docker build -t docintelapp/webapp:latest  \
    -t docintelapp/webapp:$MAJOR  \
    -t docintelapp/webapp:$MINOR  \
    -f ./DocIntel.WebApp/Dockerfile .
fi