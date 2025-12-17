#!/usr/bin/env python3
"""
Query Azure AI Search to see what content is actually indexed
"""
from azure.core.credentials import AzureKeyCredential
from azure.search.documents import SearchClient
import os
import sys

# Configuration
endpoint = "https://search-doc-class-1764130250.search.windows.net"
key = os.environ.get("SEARCH_ADMIN_KEY", "YOUR_SEARCH_ADMIN_KEY")
index_name = "documents-index"

try:
    credential = AzureKeyCredential(key)
    client = SearchClient(endpoint=endpoint, index_name=index_name, credential=credential)
    
    # Query all documents
    results = client.search(search_text="*", select=["id", "fileName", "content", "documentType"], top=10)
    
    print(f"\n{'='*100}")
    print(f"📊 Azure AI Search Index - {index_name}")
    print(f"{'='*100}\n")
    
    count = 0
    for result in results:
        count += 1
        filename = result.get('fileName', 'N/A')
        doc_type = result.get('documentType', 'N/A')
        content = result.get('content', '')
        content_preview = content[:300] if content else "No content"
        
        print(f"Document #{count}: {filename} ({doc_type})")
        print(f"Content Preview: {content_preview}")
        if len(content) > 300:
            print(f"... (Total length: {len(content)} characters)")
        print("-" * 100)
    
    print(f"\nTotal documents found: {count}\n")
    
except Exception as e:
    print(f"Error: {e}")
    print("\nTo install required package, run:")
    print("pip install azure-search-documents")
