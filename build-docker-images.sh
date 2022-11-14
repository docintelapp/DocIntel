echo "📦 Building DocIntel.Services.DocumentAnalyzer"
docker build -t docintelapp/document-analyzer -f ./DocIntel.Services.DocumentAnalyzer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.DocumentIndexer"
docker build -t docintelapp/document-indexer -f ./DocIntel.Services.DocumentIndexer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Importer"
docker build -t docintelapp/importer -f ./DocIntel.Services.Importer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Newsletters"
docker build -t docintelapp/newsletter -f ./DocIntel.Services.Newsletters/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Scraper"
docker build -t docintelapp/scraper -f ./DocIntel.Services.Scraper/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.SourceIndexer"
docker build -t docintelapp/source-indexer -f ./DocIntel.Services.SourceIndexer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.TagIndexer"
docker build -t docintelapp/tag-indexer -f ./DocIntel.Services.TagIndexer/Dockerfile .

echo ""
echo "📦 Building DocIntel.Services.Thumbnailer"
docker build -t docintelapp/thumbnailer -f ./DocIntel.Services.Thumbnailer/Dockerfile .

echo ""
echo "📦 Building DocIntel.WebApp"
docker build -t docintelapp/webapp -f ./DocIntel.WebApp/Dockerfile .
