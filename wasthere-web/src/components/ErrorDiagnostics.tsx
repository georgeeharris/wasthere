import { useState, useCallback } from 'react';
import type { DiagnosticInfo } from '../types';

interface ErrorDiagnosticsProps {
  error: string;
  diagnostics?: DiagnosticInfo;
  onClose: () => void;
}

const COPY_SUCCESS_DURATION_MS = 3000;

export function ErrorDiagnostics({ error, diagnostics, onClose }: ErrorDiagnosticsProps) {
  const [showDetails, setShowDetails] = useState(false);
  const [copied, setCopied] = useState(false);

  const formatDiagnostics = useCallback(() => {
    if (!diagnostics) {
      return `Error: ${error}\n\nNo diagnostic information available.`;
    }

    let output = `Error Report\n`;
    output += `============\n\n`;
    output += `Error Message: ${error}\n\n`;

    if (diagnostics.errorMessage) {
      output += `Detailed Error: ${diagnostics.errorMessage}\n\n`;
    }

    if (diagnostics.metadata && Object.keys(diagnostics.metadata).length > 0) {
      output += `Metadata:\n`;
      output += `---------\n`;
      for (const [key, value] of Object.entries(diagnostics.metadata)) {
        output += `  ${key}: ${value}\n`;
      }
      output += `\n`;
    }

    if (diagnostics.steps && diagnostics.steps.length > 0) {
      output += `Processing Steps:\n`;
      output += `-----------------\n`;
      for (const step of diagnostics.steps) {
        output += `\n[${step.status.toUpperCase()}] ${step.name}\n`;
        output += `  Time: ${new Date(step.timestamp).toISOString()}\n`;
        if (step.durationMs !== null && step.durationMs !== undefined) {
          output += `  Duration: ${step.durationMs}ms\n`;
        }
        if (step.details) {
          output += `  Details: ${step.details}\n`;
        }
        if (step.error) {
          output += `  Error: ${step.error}\n`;
        }
      }
      output += `\n`;
    }

    if (diagnostics.stackTrace) {
      output += `Stack Trace:\n`;
      output += `------------\n`;
      output += diagnostics.stackTrace;
      output += `\n\n`;
    }

    output += `\nBrowser Information:\n`;
    output += `--------------------\n`;
    output += `User Agent: ${navigator.userAgent}\n`;
    output += `Timestamp: ${new Date().toISOString()}\n`;

    return output;
  }, [error, diagnostics]);

  const copyToClipboard = async () => {
    const diagnosticText = formatDiagnostics();
    try {
      await navigator.clipboard.writeText(diagnosticText);
      setCopied(true);
      setTimeout(() => setCopied(false), COPY_SUCCESS_DURATION_MS);
    } catch (err) {
      console.error('Failed to copy:', err);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content error-diagnostics" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h3>❌ Upload Failed</h3>
          <button onClick={onClose} className="modal-close">&times;</button>
        </div>
        
        <div className="modal-body">
          <div className="error-message">
            <strong>Error:</strong> {error}
          </div>

          {diagnostics && (
            <>
              <div className="diagnostics-toggle">
                <button 
                  onClick={() => setShowDetails(!showDetails)}
                  className="btn btn-small"
                >
                  {showDetails ? '▼ Hide' : '▶ Show'} Diagnostics
                </button>
                <button 
                  onClick={copyToClipboard}
                  className="btn btn-small btn-primary"
                  disabled={copied}
                >
                  {copied ? '✓ Copied!' : '📋 Copy Diagnostics'}
                </button>
              </div>

              {showDetails && (
                <div className="diagnostics-details">
                  {diagnostics.metadata && Object.keys(diagnostics.metadata).length > 0 && (
                    <div className="diagnostics-section">
                      <h4>Metadata</h4>
                      <table className="diagnostics-table">
                        <tbody>
                          {Object.entries(diagnostics.metadata).map(([key, value]) => (
                            <tr key={key}>
                              <td className="key">{key}</td>
                              <td className="value">{value}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}

                  {diagnostics.steps && diagnostics.steps.length > 0 && (
                    <div className="diagnostics-section">
                      <h4>Processing Steps</h4>
                      <div className="steps-list">
                        {diagnostics.steps.map((step, index) => (
                          <div key={index} className={`step step-${step.status}`}>
                            <div className="step-header">
                              <span className="step-status">
                                {step.status === 'completed' && '✓'}
                                {step.status === 'failed' && '✗'}
                                {step.status === 'started' && '○'}
                              </span>
                              <span className="step-name">{step.name}</span>
                              {step.durationMs !== null && step.durationMs !== undefined && (
                                <span className="step-duration">{step.durationMs}ms</span>
                              )}
                            </div>
                            {step.details && <div className="step-details">{step.details}</div>}
                            {step.error && <div className="step-error">Error: {step.error}</div>}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  {diagnostics.errorMessage && (
                    <div className="diagnostics-section">
                      <h4>Detailed Error</h4>
                      <pre className="error-text">{diagnostics.errorMessage}</pre>
                    </div>
                  )}

                  {diagnostics.stackTrace && (
                    <div className="diagnostics-section">
                      <h4>Stack Trace</h4>
                      <pre className="stack-trace">{diagnostics.stackTrace}</pre>
                    </div>
                  )}
                </div>
              )}
            </>
          )}

          <div className="diagnostics-help">
            <p><strong>What to do next:</strong></p>
            <ul>
              <li>Click "Copy Diagnostics" to copy detailed error information</li>
              <li>Share the diagnostics with support or development team</li>
              <li>Try uploading a different image</li>
              <li>Check your network connection</li>
            </ul>
          </div>
        </div>

        <div className="modal-footer">
          <button onClick={onClose} className="btn">Close</button>
        </div>
      </div>
    </div>
  );
}
