MAJOR=$1
MINOR=$2

echo "📦 Pushing DocIntel.Services.DocumentAnalyzer"
docker push docintelapp/document-analyzer:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/document-analyzer:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/document-analyzer:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.DocumentIndexer"
docker push docintelapp/document-indexer:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/document-indexer:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/document-indexer:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.Importer"
docker push docintelapp/importer:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/importer:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/importer:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.Newsletters"
docker push docintelapp/newsletter:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/newsletter:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/newsletter:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.Scraper"
docker push docintelapp/scraper:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/scraper:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/scraper:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.SourceIndexer"
docker push docintelapp/source-indexer:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/source-indexer:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/source-indexer:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.TagIndexer"
docker push docintelapp/tag-indexer:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/tag-indexer:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/tag-indexer:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.Thumbnailer"
docker push docintelapp/thumbnailer:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/thumbnailer:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/thumbnailer:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.Services.Cron"
docker push docintelapp/cron:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/cron:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/cron:$MINOR
fi

echo ""
echo "📦 Pushing DocIntel.WebApp"
docker push docintelapp/webapp:latest
if [ -z "$MAJOR" ]; then 
  docker push docintelapp/webapp:$MAJOR
fi
if [ -z "$MINOR" ]; then 
  docker push docintelapp/webapp:$MINOR
fi