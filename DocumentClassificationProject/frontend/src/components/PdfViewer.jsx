import React, { useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/Card';
import { AlertCircle, FileText, ExternalLink } from 'lucide-react';
import { Button } from './ui/Button';

const PdfViewer = ({ url, fileName }) => {
    const [error, setError] = useState(false);

    if (!url) {
        return (
            <div className="h-full flex flex-col items-center justify-center text-muted-foreground p-8 border-2 border-dashed rounded-lg">
                <FileText className="h-12 w-12 mb-4 opacity-20" />
                <p>Select a document to view</p>
            </div>
        );
    }

    if (error) {
        return (
            <Card className="h-full flex flex-col items-center justify-center p-8 text-center">
                <AlertCircle className="h-12 w-12 text-destructive mb-4" />
                <h3 className="text-lg font-semibold mb-2">Cannot Preview Document</h3>
                <p className="text-muted-foreground mb-4 max-w-xs">
                    The document might be private or blocked by browser security settings.
                </p>
                <Button variant="outline" onClick={() => window.open(url, '_blank')}>
                    <ExternalLink className="h-4 w-4 mr-2" />
                    Open in New Tab
                </Button>
            </Card>
        );
    }

    return (
        <div className="h-full flex flex-col bg-card rounded-lg border overflow-hidden">
            <div className="p-2 border-b bg-muted/30 flex justify-between items-center">
                <span className="text-sm font-medium truncate px-2">{fileName}</span>
                <Button variant="ghost" size="sm" onClick={() => window.open(url, '_blank')}>
                    <ExternalLink className="h-4 w-4" />
                </Button>
            </div>
            <iframe
                src={url}
                className="flex-1 w-full h-full bg-white"
                title="PDF Viewer"
                onError={() => setError(true)}
            />
        </div>
    );
};

export default PdfViewer;
