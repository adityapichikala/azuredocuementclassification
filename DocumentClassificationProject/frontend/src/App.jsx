import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import Layout from './components/Layout';
import FileUpload from './FileUpload';
import ChatInterface from './ChatInterface';
import PdfViewer from './components/PdfViewer';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from './components/ui/Card';
import { Button } from './components/ui/Button';
import { FileText, Check, Eye, MessageSquare } from 'lucide-react';
import { cn } from './lib/utils';

function App() {
  const [activeTab, setActiveTab] = useState('documents');
  const [documents, setDocuments] = useState([]);
  const [selectedDocuments, setSelectedDocuments] = useState([]);
  const [viewingDoc, setViewingDoc] = useState(null);
  const chatRef = useRef(null);

  const fetchDocuments = async () => {
    try {
      const response = await axios.get('http://localhost:7071/api/GetDocuments');
      setDocuments(response.data);
    } catch (error) {
      console.error("Error fetching documents:", error);
    }
  };

  useEffect(() => {
    fetchDocuments();
  }, []);

  const handleUploadSuccess = async (filename) => {
    console.log(`File uploaded: ${filename}`);
    await fetchDocuments();

    // Auto-select the uploaded document
    if (!selectedDocuments.includes(filename)) {
      setSelectedDocuments(prev => [...prev, filename]);
    }

    // Switch to chat and trigger summary
    setActiveTab('chat');

    // Small delay to ensure ref is ready and tab is switched
    setTimeout(() => {
      if (chatRef.current) {
        chatRef.current.sendMessage(`Please provide a 3-bullet summary of the document "${filename}".`);
      }
    }, 500);
  };

  const toggleDocumentSelection = (fileName) => {
    setSelectedDocuments(prev =>
      prev.includes(fileName)
        ? prev.filter(f => f !== fileName)
        : [...prev, fileName]
    );
  };

  const handleViewDocument = (doc) => {
    setViewingDoc(doc);
    setActiveTab('chat');
  };

  const renderContent = () => {
    switch (activeTab) {

      case 'documents':
        return (
          <div className="space-y-6">
            <div>
              <h2 className="text-3xl font-bold tracking-tight">Documents</h2>
              <p className="text-muted-foreground">Manage your knowledge base.</p>
            </div>

            <div className="grid gap-6 md:grid-cols-2">
              <Card>
                <CardHeader>
                  <CardTitle>Upload New Document</CardTitle>
                  <CardDescription>Supported formats: PDF, JPEG, PNG, TIFF, BMP</CardDescription>
                </CardHeader>
                <CardContent>
                  <FileUpload onUploadSuccess={handleUploadSuccess} />
                </CardContent>
              </Card>

              <Card>
                <CardHeader>
                  <CardTitle>Available Documents</CardTitle>
                  <CardDescription>Select documents to include in chat context</CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-2 max-h-[400px] overflow-y-auto pr-2">
                    {documents.length === 0 ? (
                      <div className="flex flex-col items-center justify-center py-8 text-center text-muted-foreground">
                        <FileText className="h-12 w-12 mb-2 opacity-20" />
                        <p>No documents found</p>
                      </div>
                    ) : (
                      documents.map(doc => (
                        <div
                          key={doc.documentId}
                          className={cn(
                            "flex items-center justify-between p-3 rounded-lg border transition-all hover:bg-accent group",
                            selectedDocuments.includes(doc.fileName)
                              ? "border-primary bg-primary/5"
                              : "border-border"
                          )}
                        >
                          <div
                            className="flex items-center space-x-3 overflow-hidden flex-1 cursor-pointer"
                            onClick={() => toggleDocumentSelection(doc.fileName)}
                          >
                            <div className={cn(
                              "p-2 rounded-full transition-colors",
                              selectedDocuments.includes(doc.fileName) ? "bg-primary/10 text-primary" : "bg-muted text-muted-foreground"
                            )}>
                              <FileText className="h-4 w-4" />
                            </div>
                            <div className="min-w-0 flex-1">
                              <p className="text-sm font-medium break-all">{doc.fileName}</p>
                              <p className="text-xs text-muted-foreground">
                                {doc.timestamp ? new Date(doc.timestamp).toLocaleDateString() : 'Unknown date'}
                              </p>
                            </div>
                          </div>

                          <div className="flex items-center space-x-2">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="h-8 w-8 opacity-0 group-hover:opacity-100 transition-opacity"
                              onClick={(e) => {
                                e.stopPropagation();
                                handleViewDocument(doc);
                              }}
                              title="View Document"
                            >
                              <Eye className="h-4 w-4" />
                            </Button>
                            {selectedDocuments.includes(doc.fileName) && (
                              <Check className="h-4 w-4 text-primary" />
                            )}
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        );

      case 'chat':
        return (
          <div className="h-[calc(100vh-8rem)] flex flex-col">
            <div className="mb-4 flex justify-between items-center">
              <div>
                <h2 className="text-3xl font-bold tracking-tight">Chat</h2>
                <p className="text-muted-foreground">
                  {selectedDocuments.length > 0
                    ? `Chatting with ${selectedDocuments.length} selected document(s)`
                    : "Select documents to start chatting"}
                </p>
              </div>
              {viewingDoc && (
                <Button variant="outline" size="sm" onClick={() => setViewingDoc(null)}>
                  Close Viewer
                </Button>
              )}
            </div>

            <div className="flex-1 min-h-0 flex gap-6">
              {/* Split Screen: PDF Viewer */}
              {viewingDoc && (
                <div className="w-1/2 min-w-[300px] h-full animate-in slide-in-from-left duration-300">
                  <PdfViewer url={viewingDoc.blobUrl} fileName={viewingDoc.fileName} />
                </div>
              )}

              {/* Chat Interface */}
              <div className={cn(
                "h-full transition-all duration-300",
                viewingDoc ? "w-1/2" : "w-full max-w-3xl mx-auto"
              )}>
                <ChatInterface
                  ref={chatRef}
                  selectedDocuments={selectedDocuments}
                />
              </div>
            </div>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <Layout activeTab={activeTab} onTabChange={setActiveTab}>
      {renderContent()}
    </Layout>
  );
}

export default App;
