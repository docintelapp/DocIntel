echo "📦 Building DocIntel.Services.DocumentAnalyzer"
docker build -t docintelapp/document-analyzer:latest -t docintelapp/document-analyzer:v2.1 -t docintelapp/document-analyzer:v2.1.1 -f ./DocIntel.Services.DocumentAnalyzer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.DocumentIndexer"
docker build -t docintelapp/document-indexer:latest -t docintelapp/document-indexer:v2.1 -t docintelapp/document-indexer:v2.1.1 -f ./DocIntel.Services.DocumentIndexer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Importer"
docker build -t docintelapp/importer:latest -t docintelapp/importer:v2.1 -t docintelapp/importer:v2.1.1 -f ./DocIntel.Services.Importer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Newsletters"
docker build -t docintelapp/newsletter:latest -t docintelapp/newsletter:v2.1 -t docintelapp/newsletter:v2.1.1 -f ./DocIntel.Services.Newsletters/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Scraper"
docker build -t docintelapp/scraper:latest -t docintelapp/scraper:v2.1 -t docintelapp/scraper:v2.1.1 -f ./DocIntel.Services.Scraper/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.SourceIndexer"
docker build -t docintelapp/source-indexer:latest -t docintelapp/source-indexer:v2.1 -t docintelapp/source-indexer:v2.1.1 -f ./DocIntel.Services.SourceIndexer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.TagIndexer"
docker build -t docintelapp/tag-indexer:latest -t docintelapp/tag-indexer:v2.1 -t docintelapp/tag-indexer:v2.1.1 -f ./DocIntel.Services.TagIndexer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Thumbnailer"
docker build -t docintelapp/thumbnailer:latest -t docintelapp/thumbnailer:v2.1 -t docintelapp/thumbnailer:v2.1.1 -f ./DocIntel.Services.Thumbnailer/Dockerfile .

echo ""
echo "📦 Building DocIntel.WebApp"
docker build -t docintelapp/webapp:latest -t docintelapp/webapp:v2.1 -t docintelapp/webapp:v2.1.1 -f ./DocIntel.WebApp/Dockerfile .
