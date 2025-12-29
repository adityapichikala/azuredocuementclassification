from azure.storage.blob import BlobServiceClient
import os

# Get connection string from environment variable
conn_str = os.getenv("AzureWebJobsStorage")
if not conn_str:
    raise ValueError("AzureWebJobsStorage environment variable is not set")

try:
    blob_service_client = BlobServiceClient.from_connection_string(conn_str)
    containers = blob_service_client.list_containers()
    print("Containers:")
    for c in containers:
        print(f"- {c.name}")
        container_client = blob_service_client.get_container_client(c.name)
        blobs = container_client.list_blobs()
        print(f"  Blobs in {c.name}:")
        count = 0
        for b in blobs:
            if count < 5:
                print(f"    - {b.name}")
            count += 1
        if count >= 5:
            print(f"    ... (Total: {count})")
except Exception as e:
    print(f"Error: {e}")
