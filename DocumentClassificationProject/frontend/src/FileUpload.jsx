import React, { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import axios from 'axios';

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
            // Send file as binary body, pass filename in query param
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
        <div className="upload-container">
            <div {...getRootProps()} className={`dropzone ${isDragActive ? 'active' : ''}`}>
                <input {...getInputProps()} />
                {uploading ? (
                    <p>Uploading...</p>
                ) : isDragActive ? (
                    <p>Drop the file here ...</p>
                ) : (
                    <div>
                        <p>Drag 'n' drop a file here, or click to select a file</p>
                        <p className="supported-types">Supported: PDF, JPEG, PNG, TIFF, BMP</p>
                    </div>
                )}
            </div>
            {message && <div className="success-message">{message}</div>}
            {error && <div className="error-message">{error}</div>}

            <style>{`
        .upload-container {
          margin-bottom: 2rem;
        }
        .dropzone {
          border: 2px dashed var(--border-color);
          border-radius: 8px;
          padding: 2rem;
          text-align: center;
          background-color: var(--surface-color);
          cursor: pointer;
          transition: border-color 0.2s ease;
        }
        .dropzone:hover, .dropzone.active {
          border-color: var(--primary-color);
          background-color: rgba(0, 120, 212, 0.05);
        }
        .supported-types {
          font-size: 0.85rem;
          color: var(--text-secondary);
          margin-top: 0.5rem;
        }
        .success-message {
          color: var(--success-color);
          margin-top: 0.5rem;
          font-size: 0.9rem;
        }
        .error-message {
          color: var(--error-color);
          margin-top: 0.5rem;
          font-size: 0.9rem;
        }
      `}</style>
        </div>
    );
};

export default FileUpload;
