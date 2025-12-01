#!/usr/bin/env python3
"""
Query Cosmos DB documents using Azure SDK
"""
from azure.cosmos import CosmosClient
import os

# Configuration
endpoint = "https://cosmos-doc-class-1764130250.documents.azure.com:443/"
key = os.environ.get("COSMOS_DB_KEY", "YOUR_COSMOS_DB_KEY")
database_name = "DocumentMetadata"
container_name = "Documents"

try:
    # Create Cosmos client
    client = CosmosClient(endpoint, key)
    
    # Get database and container
    database = client.get_database_client(database_name)
    container = database.get_container_client(container_name)
    
    # Query all documents
    query = "SELECT c.id, c.fileName, c.documentType, c.uploadDate, c.blobUrl FROM c"
    items = list(container.query_items(query=query, enable_cross_partition_query=True))
    
    print(f"\n{'='*100}")
    print(f"📊 Cosmos DB - {database_name}/{container_name}")
    print(f"{'='*100}")
    print(f"Total Documents: {len(items)}\n")
    
    # Print table header
    print(f"{'#':<4} {'Document ID':<30} {'Filename':<40} {'Type':<6} {'Upload Date'}")
    print('-' * 100)
    
    # Print each document
    for i, item in enumerate(items, 1):
        doc_id = item.get('id', 'N/A')[:28]
        filename = item.get('fileName', 'N/A')[:38]
        doc_type = item.get('documentType', 'N/A')
        upload_date = item.get('uploadDate', 'N/A')[:19]
        
        print(f"{i:<4} {doc_id:<30} {filename:<40} {doc_type:<6} {upload_date}")
    
    print(f"\n{'='*100}\n")
    
except Exception as e:
    print(f"Error: {e}")
    print("\nTo install required package, run:")
    print("pip install azure-cosmos")
