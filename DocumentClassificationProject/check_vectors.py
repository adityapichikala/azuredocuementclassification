import os
from azure.core.credentials import AzureKeyCredential
from azure.search.documents import SearchClient

endpoint = "https://search-doc-class-1764130250.search.windows.net"
key = os.environ.get("SEARCH_ADMIN_KEY", "YOUR_SEARCH_ADMIN_KEY")
index_name = "documents-index"

credential = AzureKeyCredential(key)
client = SearchClient(endpoint=endpoint, index_name=index_name, credential=credential)

results = client.search(search_text="*", select=["id", "fileName", "contentVector"], top=5)

print(f"Checking index {index_name}...")
count = 0
for result in results:
    count += 1
    has_vector = result.get('contentVector') is not None
    vector_len = len(result['contentVector']) if has_vector else 0
    print(f"Doc: {result['fileName']} | Has Vector: {has_vector} | Vector Len: {vector_len}")

print(f"Total docs checked: {count}")
