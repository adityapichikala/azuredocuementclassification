import time
import os
import urllib.parse
import hmac
import hashlib
import base64
import json
import urllib.request

def get_auth_token(sb_name, sas_key_name, sas_key):
    uri = urllib.parse.quote_plus(f"https://{sb_name}.servicebus.windows.net/document-queue")
    sas = sas_key.encode('utf-8')
    expiry = str(int(time.time() + 3600))
    string_to_sign = (uri + '\n' + expiry).encode('utf-8')
    signed_hmac_sha256 = hmac.new(sas, string_to_sign, hashlib.sha256)
    signature = urllib.parse.quote(base64.b64encode(signed_hmac_sha256.digest()))
    return f"SharedAccessSignature sr={uri}&sig={signature}&se={expiry}&skn={sas_key_name}"

sb_name = os.environ.get("SERVICE_BUS_NAMESPACE", "sb-doc-class-1764130250")
sas_key_name = os.environ.get("SERVICE_BUS_KEY_NAME", "RootManageSharedAccessKey")
sas_key = os.environ.get("SERVICE_BUS_SAS_KEY", "YOUR_SAS_KEY")

token = get_auth_token(sb_name, sas_key_name, sas_key)

url = f"https://{sb_name}.servicebus.windows.net/document-queue/messages"
headers = {
    "Authorization": token,
    "Content-Type": "application/json"
}
data = {
    "BlobUrl": "https://stdocclass1764130250.blob.core.windows.net/documents/search-test.pdf",
    "DocumentId": "sb-test-python",
    "FileName": "search-test.pdf"
}

print(f"Sending message to {url}...")
req = urllib.request.Request(url, data=json.dumps(data).encode('utf-8'), headers=headers, method='POST')
try:
    with urllib.request.urlopen(req) as response:
        print(f"Status: {response.status}")
        print("Message sent successfully!")
except urllib.error.HTTPError as e:
    print(f"Error: {e.code}")
    print(e.read().decode('utf-8'))
