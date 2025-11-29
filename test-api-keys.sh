#!/bin/bash

echo "════════════════════════════════════════════════════════"
echo "   TESTING GEMINI API KEYS"
echo "════════════════════════════════════════════════════════"
echo ""

# Array of API keys to test
API_KEYS=(
    "AIzaSyBLzuTObWlZHqZMXcfEAfNb8qLwnfNk0zU"
    "AIzaSyAic4Wem2qrZg0NkKa8VtVN7jyJ5aaKx6k"
)

MODEL="gemini-2.5-flash"
TEST_MESSAGE="Hello"

for i in "${!API_KEYS[@]}"; do
    KEY="${API_KEYS[$i]}"
    INDEX=$((i + 1))
    
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Testing API Key #$INDEX: ${KEY:0:15}..."
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    
    RESPONSE=$(curl -s -w "\nHTTP_CODE:%{http_code}" \
        -X POST "https://generativelanguage.googleapis.com/v1beta/models/${MODEL}:generateContent?key=${KEY}" \
        -H "Content-Type: application/json" \
        -d "{\"contents\":[{\"parts\":[{\"text\":\"${TEST_MESSAGE}\"}]}]}" 2>&1)
    
    HTTP_CODE=$(echo "$RESPONSE" | grep "HTTP_CODE:" | cut -d: -f2)
    BODY=$(echo "$RESPONSE" | grep -v "HTTP_CODE:")
    
    echo "HTTP Status: $HTTP_CODE"
    
    if [ "$HTTP_CODE" = "200" ]; then
        echo "✅ STATUS: WORKING - API Key is valid and has quota"
        # Extract response text if available
        RESPONSE_TEXT=$(echo "$BODY" | grep -o '"text":"[^"]*"' | head -1 | cut -d'"' -f4)
        if [ ! -z "$RESPONSE_TEXT" ]; then
            echo "📝 Response: $RESPONSE_TEXT"
        fi
    elif [ "$HTTP_CODE" = "429" ]; then
        echo "❌ STATUS: QUOTA EXCEEDED - Too many requests"
        echo "💡 This key has hit the rate limit or daily quota"
    elif [ "$HTTP_CODE" = "400" ]; then
        echo "⚠️  STATUS: BAD REQUEST - Model may not be available or invalid request"
        ERROR_MSG=$(echo "$BODY" | grep -o '"message":"[^"]*"' | head -1 | cut -d'"' -f4)
        if [ ! -z "$ERROR_MSG" ]; then
            echo "📋 Error: $ERROR_MSG"
        fi
    elif [ "$HTTP_CODE" = "403" ]; then
        echo "❌ STATUS: FORBIDDEN - API Key is invalid or doesn't have access"
    else
        echo "⚠️  STATUS: UNKNOWN ERROR"
        echo "Response: $BODY"
    fi
    
    echo ""
done

echo "════════════════════════════════════════════════════════"
echo "   TEST COMPLETE"
echo "════════════════════════════════════════════════════════"

