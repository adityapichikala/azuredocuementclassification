import os

def create_dummy_data():
    base_dir = "training-data"
    categories = ["invoice", "contract"]
    
    if not os.path.exists(base_dir):
        os.makedirs(base_dir)
        
    for category in categories:
        cat_dir = os.path.join(base_dir, category)
        if not os.path.exists(cat_dir):
            os.makedirs(cat_dir)
            
        print(f"Generating {category} samples...")
        for i in range(1, 6):
            filename = os.path.join(cat_dir, f"{category}_{i}.html")
            with open(filename, "w") as f:
                if category == "invoice":
                    content = f"<html><body><h1>INVOICE #{1000+i}</h1><p>Date: 2024-01-{i:02d}</p><p>Vendor: Vendor {i}</p><p>Total: ${i*100}.00</p><p>Please pay by due date.</p></body></html>"
                else:
                    content = f"<html><body><h1>CONTRACT AGREEMENT</h1><p>This contract is made on 2024-01-{i:02d} between Party A and Party B.</p><h2>Terms and Conditions</h2><ol><li>Clause {i}</li><li>Clause {i+1}</li></ol><p>Signed: ________________</p></body></html>"
                
                f.write(content)
    
    print(f"\n✅ Generated 10 sample files in '{base_dir}' directory.")
    print("\nNext Steps:")
    print(f"1. Upload these files to Azure Storage:")
    print(f"   az storage blob upload-batch --destination training-data --source {base_dir} --account-name <YOUR_STORAGE_ACCOUNT> --auth-mode login")

if __name__ == "__main__":
    create_dummy_data()
