import React, { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import axios from 'axios';
import { Upload, File, CheckCircle, AlertCircle, Loader2 } from 'lucide-react';
import { cn } from './lib/utils';

const FileUpload = ({ onUploadSuccess }) => {
    const [uploading, setUploading] = useState(false);
    const [message, setMessage] = useState(null);
    const [error, setError] = useState(null);

    const onDrop = useCallback(async (acceptedFiles) => {
        const file = acceptedFiles[0];
        if (!file) return;

        setUploading(true);
        setMessage(null);
        setError(null);

        try {
            const response = await axios.post(`http://localhost:7071/api/upload?filename=${encodeURIComponent(file.name)}`, file, {
                headers: {
                    'Content-Type': file.type,
                },
            });
            setMessage(`Successfully uploaded ${file.name}`);
            if (onUploadSuccess) {
                onUploadSuccess(file.name);
            }
        } catch (err) {
            console.error(err);
            setError('Failed to upload file. Please try again.');
        } finally {
            setUploading(false);
        }
    }, [onUploadSuccess]);

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: {
            'application/pdf': ['.pdf'],
            'image/jpeg': ['.jpg', '.jpeg'],
            'image/png': ['.png'],
            'image/tiff': ['.tiff', '.tif'],
            'image/bmp': ['.bmp']
        },
        multiple: false
    });

    return (
        <div className="w-full">
            <div
                {...getRootProps()}
                className={cn(
                    "border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors duration-200 ease-in-out flex flex-col items-center justify-center min-h-[200px]",
                    isDragActive
                        ? "border-primary bg-primary/5"
                        : "border-muted-foreground/25 hover:border-primary/50 hover:bg-accent"
                )}
            >
                <input {...getInputProps()} />
                {uploading ? (
                    <div className="flex flex-col items-center space-y-2 text-muted-foreground">
                        <Loader2 className="h-10 w-10 animate-spin text-primary" />
                        <p>Uploading...</p>
                    </div>
                ) : isDragActive ? (
                    <div className="flex flex-col items-center space-y-2 text-primary">
                        <Upload className="h-10 w-10" />
                        <p className="font-medium">Drop the file here ...</p>
                    </div>
                ) : (
                    <div className="flex flex-col items-center space-y-2 text-muted-foreground">
                        <Upload className="h-10 w-10 mb-2" />
                        <p className="font-medium text-foreground">Drag 'n' drop a file here, or click to select</p>
                        <p className="text-xs">Supported: PDF, JPEG, PNG, TIFF, BMP</p>
                    </div>
                )}
            </div>

            {message && (
                <div className="mt-4 p-3 rounded-md bg-green-500/10 text-green-600 flex items-center text-sm">
                    <CheckCircle className="h-4 w-4 mr-2" />
                    {message}
                </div>
            )}

            {error && (
                <div className="mt-4 p-3 rounded-md bg-destructive/10 text-destructive flex items-center text-sm">
                    <AlertCircle className="h-4 w-4 mr-2" />
                    {error}
                </div>
            )}
        </div>
    );
};

export default FileUpload;
