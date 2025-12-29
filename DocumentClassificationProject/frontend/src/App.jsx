import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';
import Layout from './components/Layout';
import FileUpload from './FileUpload';
import ChatInterface from './ChatInterface';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from './components/ui/Card';
import { Button } from './components/ui/Button';
import { FileText, Check, MessageSquare, PieChart, BarChart, Trash2, Loader2 } from 'lucide-react';
import { cn } from './lib/utils';

function App() {
  const [activeTab, setActiveTab] = useState('documents');
  const [documents, setDocuments] = useState([]);
  const [selectedDocuments, setSelectedDocuments] = useState([]);
  const [processingDocument, setProcessingDocument] = useState(null);
  const [processingStatus, setProcessingStatus] = useState('');
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

    // Set processing state
    setProcessingDocument(filename);
    setProcessingStatus('Processing document...');

    // Poll for document readiness
    const maxAttempts = 15; // 15 attempts * 2 seconds = 30 seconds max
    let attempts = 0;
    let documentReady = false;

    const pollInterval = setInterval(async () => {
      attempts++;

      try {
        const response = await axios.get('http://localhost:7071/api/GetDocuments');
        const docs = response.data;

        // Check if our document is in the list
        const uploadedDoc = docs.find(d => d.fileName === filename);

        if (uploadedDoc) {
          // Document found! Clear polling
          clearInterval(pollInterval);
          documentReady = true;

          // Update UI
          setProcessingStatus('Document ready!');
          await fetchDocuments();

          // Auto-select the document
          if (!selectedDocuments.includes(filename)) {
            setSelectedDocuments(prev => [...prev, filename]);
          }

          // Small delay to show "ready" status
          setTimeout(() => {
            setProcessingDocument(null);
            setActiveTab('chat');

            // Send summary request
            setTimeout(() => {
              if (chatRef.current) {
                chatRef.current.sendMessage(`Please provide a 3-bullet summary of the document "${filename}".`);
              }
            }, 500);
          }, 1000);
        } else {
          // Still processing
          setProcessingStatus(`Processing document... (${attempts}/${maxAttempts})`);
        }

        // Timeout check
        if (attempts >= maxAttempts && !documentReady) {
          clearInterval(pollInterval);
          setProcessingStatus('Processing timeout. Please check the backend logs.');

          // Reset after showing error
          setTimeout(() => {
            setProcessingDocument(null);
          }, 5000);
        }
      } catch (error) {
        console.error('Error polling for document:', error);
      }
    }, 2000); // Poll every 2 seconds
  };

  const toggleDocumentSelection = (fileName) => {
    setSelectedDocuments(prev =>
      prev.includes(fileName)
        ? prev.filter(f => f !== fileName)
        : [...prev, fileName]
    );
  };

  const handleDelete = async (doc) => {
    if (!confirm(`Are you sure you want to delete "${doc.fileName}"? This cannot be undone.`)) return;

    try {
      await axios.delete('http://localhost:7071/api/DeleteDocument', {
        data: {
          documentId: doc.documentId,
          blobUrl: doc.blobUrl
        }
      });
      // Remove from local state
      setDocuments(documents.filter(d => d.documentId !== doc.documentId));
      setSelectedDocuments(selectedDocuments.filter(f => f !== doc.fileName));
    } catch (error) {
      console.error('Error deleting document:', error);
      alert('Failed to delete document');
    }
  };

  // Calculate stats for dashboard
  const getDocumentStats = () => {
    const stats = documents.reduce((acc, doc) => {
      const type = doc.documentType || 'Unknown';
      acc[type] = (acc[type] || 0) + 1;
      return acc;
    }, {});
    return stats;
  };

  const renderContent = () => {
    switch (activeTab) {

      case 'dashboard':
        const stats = getDocumentStats();
        const totalDocs = documents.length;

        return (
          <div className="space-y-6">
            <div>
              <h2 className="text-3xl font-bold tracking-tight">Dashboard</h2>
              <p className="text-muted-foreground">Overview of your document classification.</p>
            </div>

            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Total Documents</CardTitle>
                  <FileText className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{totalDocs}</div>
                </CardContent>
              </Card>



              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Invoices</CardTitle>
                  <PieChart className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">{stats['invoice'] || 0}</div>
                  <p className="text-xs text-muted-foreground">
                    {totalDocs > 0 ? (((stats['invoice'] || 0) / totalDocs) * 100).toFixed(0) : 0}% of total
                  </p>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Others</CardTitle>
                  <FileText className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {totalDocs - (stats['invoice'] || 0)}
                  </div>
                  <p className="text-xs text-muted-foreground">
                    Unclassified or other types
                  </p>
                </CardContent>
              </Card>
            </div>

            <Card className="col-span-4">
              <CardHeader>
                <CardTitle>Recent Activity</CardTitle>
                <CardDescription>Latest processed documents</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {documents.slice(0, 5).map(doc => (
                    <div key={doc.documentId} className="flex items-center">
                      <div className="ml-4 space-y-1">
                        <p className="text-sm font-medium leading-none">{doc.fileName}</p>
                        <p className="text-sm text-muted-foreground">
                          Classified as <span className="font-semibold text-primary">{doc.documentType || 'Unknown'}</span>
                        </p>
                      </div>
                      <div className="flex items-center space-x-2">
                        <span className={`px-2 py-1 text-xs rounded-full ${doc.documentType === 'invoice' ? 'bg-green-100 text-green-800' :
                          'bg-gray-100 text-gray-800'
                          }`}>
                          {doc.documentType || 'Unknown'}
                        </span>
                        <button
                          onClick={() => handleDelete(doc)}
                          className="p-2 text-red-500 hover:bg-red-50 rounded-full transition-colors"
                          title="Delete Document"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        );

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
                              <div className="flex items-center gap-2">
                                <p className="text-sm font-medium break-all">{doc.fileName}</p>
                                <span className="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 border-transparent bg-secondary text-secondary-foreground hover:bg-secondary/80">
                                  {doc.documentType || 'Unknown'}
                                </span>
                              </div>
                              <p className="text-xs text-muted-foreground">
                                {doc.timestamp ? new Date(doc.timestamp).toLocaleDateString() : 'Unknown date'}
                              </p>
                            </div>
                          </div>

                          <div className="flex items-center space-x-2">
                            {selectedDocuments.includes(doc.fileName) && (
                              <Check className="h-4 w-4 text-primary" />
                            )}
                            <button
                              onClick={(e) => {
                                e.stopPropagation();
                                handleDelete(doc);
                              }}
                              className="p-2 text-red-500 hover:bg-red-50 rounded-full transition-colors opacity-0 group-hover:opacity-100"
                              title="Delete Document"
                            >
                              <Trash2 className="h-4 w-4" />
                            </button>
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
            </div>

            <div className="flex-1 min-h-0 flex gap-6">
              {/* Chat Interface */}
              <div className="w-full max-w-3xl mx-auto h-full">
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

      {processingDocument && (
        <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center">
          <div className="bg-card p-8 rounded-lg shadow-lg border max-w-md w-full mx-4">
            <div className="flex flex-col items-center space-y-4">
              <Loader2 className="h-12 w-12 animate-spin text-primary" />
              <h3 className="text-lg font-semibold">Processing Document</h3>
              <p className="text-sm text-muted-foreground text-center">
                {processingStatus}
              </p>
              <p className="text-xs text-muted-foreground">
                Uploading → Classifying → Indexing
              </p>
            </div>
          </div>
        </div>
      )}
    </Layout>
  );
}

export default App;
