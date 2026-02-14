#!/bin/bash

echo "🚀 Setting up Appointment App..."

# Navigate to project directory
cd "$(dirname "$0")" || exit 1

echo "✅ Project directory verified"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
else
    echo "✅ Dependencies already installed"
fi

# Check if tailwind.config.js exists
if [ ! -f "tailwind.config.js" ]; then
    echo "🔧 Setting up Tailwind CSS..."
    npx tailwindcss init -p
fi

# Check if public directory has placeholder images
if [ ! -d "public/images" ]; then
    echo "📁 Creating public directory structure..."
    mkdir -p public/images
    echo "📸 Add your placeholder images to public/images/"
fi

echo ""
echo "🎉 Setup complete!"
echo ""
echo "Next steps:"
echo "1. Run the development server: npm run dev"
echo "2. Open your browser: http://localhost:5173"
echo "3. Check the README.md for usage examples"
echo ""
echo "📚 Additional documentation:"
echo "  - README.md - Main project documentation"
echo "  - COMPONENTS.md - Complete component reference"
echo "  - BACKEND_INTEGRATION.md - Backend API integration guide"
echo ""
