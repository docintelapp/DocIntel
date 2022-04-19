echo "📦 Building DocIntel.Services.DocumentAnalyzer"
docker build -t docintelapp/document-analyzer -f ./DocIntel.Services.DocumentAnalyzer/Dockerfile .
docker push docintelapp/document-analyzer:latest

echo ""
echo "📦 Building DocIntel.Services.DocumentIndexer"
docker build -t docintelapp/document-indexer -f ./DocIntel.Services.DocumentIndexer/Dockerfile .
docker push docintelapp/document-indexer:latest

echo ""
echo "📦 Building DocIntel.Services.Importer"
docker build -t docintelapp/importer -f ./DocIntel.Services.Importer/Dockerfile .
docker push docintelapp/importer:latest

echo ""
echo "📦 Building DocIntel.Services.Newsletters"
docker build -t docintelapp/newsletter -f ./DocIntel.Services.Newsletters/Dockerfile .
docker push docintelapp/newsletter:latest

echo ""
echo "📦 Building DocIntel.Services.Scraper"
docker build -t docintelapp/scraper -f ./DocIntel.Services.Scraper/Dockerfile .
docker push docintelapp/scraper:latest

echo ""
echo "📦 Building DocIntel.Services.SourceIndexer"
docker build -t docintelapp/source-indexer -f ./DocIntel.Services.SourceIndexer/Dockerfile .
docker push docintelapp/source-indexer:latest

echo ""
echo "📦 Building DocIntel.Services.TagIndexer"
docker build -t docintelapp/tag-indexer -f ./DocIntel.Services.TagIndexer/Dockerfile .
docker push docintelapp/tag-indexer:latest

echo ""
echo "📦 Building DocIntel.Services.Thumbnailer"
docker build -t docintelapp/thumbnailer -f ./DocIntel.Services.Thumbnailer/Dockerfile .
docker push docintelapp/thumbnailer:latest

echo ""
echo "📦 Building DocIntel.WebApp"
docker build -t docintelapp/webapp -f ./DocIntel.WebApp/Dockerfile .
docker push docintelapp/webapp:latest

