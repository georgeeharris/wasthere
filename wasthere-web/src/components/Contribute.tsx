import { useState, useEffect } from 'react';
import type { DiagnosticInfo, FlyerAnalysisResult } from '../types';
import { flyersApi, type YearSelection, type FlyerUploadResult } from '../services/api';
import { ErrorDiagnostics } from './ErrorDiagnostics';
import { YearSelectionModal } from './YearSelectionModal';
import { EventSelectionModal } from './EventSelectionModal';
import { FlyerPreviewModal } from './FlyerPreviewModal';

export function Contribute() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [diagnostics, setDiagnostics] = useState<DiagnosticInfo | undefined>(undefined);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [showPreview, setShowPreview] = useState(false);
  const [pendingFlyerResults, setPendingFlyerResults] = useState<FlyerUploadResult[]>([]);
  const [showEventSelection, setShowEventSelection] = useState(false);
  const [showYearSelection, setShowYearSelection] = useState(false);
  const [pendingAnalysis, setPendingAnalysis] = useState<{ flyerId: number; analysisResult: FlyerAnalysisResult; needsEventSelection: boolean } | null>(null);
  const [currentFlyerIndex, setCurrentFlyerIndex] = useState<number>(0);
  const [flyerEventSelections, setFlyerEventSelections] = useState<Map<number, number>>(new Map());
  const [flyerYearSelections, setFlyerYearSelections] = useState<Map<number, Map<string, number>>>(new Map());
  const [uploadingMultipleFlyers, setUploadingMultipleFlyers] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  // Detect if device is mobile
  useEffect(() => {
    const checkMobile = () => {
      const isMobileDevice = /iPhone|iPad|iPod|Android/i.test(navigator.userAgent) || window.innerWidth <= 768;
      setIsMobile(isMobileDevice);
    };
    
    checkMobile();
    window.addEventListener('resize', checkMobile);
    return () => window.removeEventListener('resize', checkMobile);
  }, []);

  const getFlyersNeedingUserInput = (flyerResults: FlyerUploadResult[]) => {
    return flyerResults.filter(fr => 
      fr.success && (fr.needsEventSelection || 
        (fr.analysisResult?.clubNights.some(cn => cn.candidateYears.length > 0) ?? false))
    );
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
      setError(null);
      setDiagnostics(undefined);
    }
  };

  const handleUpload = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!selectedFile) {
      setError('Please select a file');
      return;
    }

    setUploading(true);
    setError(null);
    setDiagnostics(undefined);
    setSuccessMessage(null);

    try {
      const result = await flyersApi.upload(selectedFile, !uploadingMultipleFlyers);
      
      if (!result.success) {
        setError(result.message || 'Failed to upload and process flyer(s)');
        setUploading(false);
        return;
      }

      const totalFlyers = result.totalFlyers;
      const flyerResults = result.flyerResults;

      console.log(`Upload complete. Processed ${totalFlyers} flyer(s)`);

      const successfulFlyers = flyerResults.filter(r => r.success && r.analysisResult);

      if (successfulFlyers.length > 0) {
        setPendingFlyerResults(successfulFlyers);
        setShowPreview(true);
        setUploading(false);
      } else {
        setSuccessMessage(result.message);
        setTimeout(() => setSuccessMessage(null), 5000);
        setUploading(false);
      }
      
      setSelectedFile(null);
      const fileInput = document.getElementById('flyer-file') as HTMLInputElement;
      if (fileInput) fileInput.value = '';
      
    } catch (error) {
      console.error('Failed to upload flyer:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to upload flyer';
      setError(errorMessage);
      setDiagnostics(undefined);
      setUploading(false);
    }
  };

  const handlePreviewConfirm = () => {
    setShowPreview(false);
    
    const flyersNeedingInput = getFlyersNeedingUserInput(pendingFlyerResults);

    if (flyersNeedingInput.length > 0) {
      const firstFlyerNeedingInput = flyersNeedingInput[0];
      
      setPendingAnalysis({
        flyerId: firstFlyerNeedingInput.flyer!.id,
        analysisResult: firstFlyerNeedingInput.analysisResult!,
        needsEventSelection: firstFlyerNeedingInput.needsEventSelection ?? false
      });
      
      setCurrentFlyerIndex(0);
      setFlyerEventSelections(new Map());
      setFlyerYearSelections(new Map());

      if (firstFlyerNeedingInput.needsEventSelection) {
        setShowEventSelection(true);
        setSuccessMessage(firstFlyerNeedingInput.message);
      } else {
        const needsYearSelection = firstFlyerNeedingInput.analysisResult?.clubNights.some(
          (cn) => !cn.date && cn.month && cn.day && cn.candidateYears.length > 0
        );
        if (needsYearSelection) {
          setShowYearSelection(true);
          setSuccessMessage(firstFlyerNeedingInput.message);
        }
      }
    } else {
      setSuccessMessage('Flyers uploaded successfully! View them in the Flyers page.');
      setTimeout(() => setSuccessMessage(null), 5000);
    }
    
    setPendingFlyerResults([]);
  };

  const handlePreviewCancel = async () => {
    setShowPreview(false);
    
    try {
      const flyerIds = pendingFlyerResults
        .map(flyerResult => flyerResult.flyer?.id)
        .filter((id): id is number => id !== undefined);
      
      const deletePromises = flyerIds.map(id => flyersApi.delete(id));
      
      const results = await Promise.allSettled(deletePromises);
      
      const failedDeletions = results.filter(result => result.status === 'rejected').length;
      
      if (failedDeletions > 0) {
        setError(`Upload cancelled. ${flyerIds.length - failedDeletions} flyer(s) deleted, but ${failedDeletions} failed. Please delete them manually.`);
      } else {
        setError('Upload cancelled. Flyers have been deleted.');
      }
      
      setPendingFlyerResults([]);
    } catch (error) {
      console.error('Failed to delete flyers:', error);
      setError('Upload cancelled, but failed to delete some flyers. Please delete them manually.');
    }
  };

  const completeUploadWithYears = async (flyerId: number, analysisResult: FlyerAnalysisResult, selectedYearsMap?: Map<string, number>, eventId?: number) => {
    try {
      const selectedYears: YearSelection[] = [];
      
      for (const cn of analysisResult.clubNights) {
        if (!cn.date && cn.month && cn.day) {
          const key = `${cn.month}-${cn.day}`;
          let year: number;
          
          if (selectedYearsMap && selectedYearsMap.has(key)) {
            year = selectedYearsMap.get(key)!;
          } else if (cn.candidateYears.length > 0) {
            year = cn.candidateYears[0];
          } else {
            continue;
          }
          
          selectedYears.push({
            month: cn.month,
            day: cn.day,
            year: year
          });
        }
      }

      const result = await flyersApi.completeUpload(flyerId, selectedYears, eventId);
      
      if (result.success) {
        setSuccessMessage(`Flyer processed! ${result.message} View them in the Flyers page.`);
        setTimeout(() => setSuccessMessage(null), 10000);
      } else {
        setError(result.message || 'Failed to process flyer');
        setDiagnostics(result.diagnostics);
      }
    } catch (error) {
      console.error('Failed to complete upload:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to complete upload';
      setError(errorMessage);
    }
  };

  const processNextFlyer = (
    flyerId: number,
    analysisResult: FlyerAnalysisResult,
    completedIndex: number,
    eventSelections: Map<number, number>,
    yearSelections: Map<number, Map<string, number>>
  ) => {
    const totalFlyers = analysisResult.flyers?.length || 1;
    const nextIndex = completedIndex + 1;

    if (nextIndex >= totalFlyers) {
      setUploading(true);
      
      const mergedYearSelections = new Map<string, number>();
      yearSelections.forEach((flyerSelections) => {
        flyerSelections.forEach((year, key) => {
          mergedYearSelections.set(key, year);
        });
      });
      
      const firstEventId = eventSelections.size > 0 ? Array.from(eventSelections.values())[0] : undefined;
      
      completeUploadWithYears(flyerId, analysisResult, mergedYearSelections, firstEventId)
        .finally(() => {
          setPendingAnalysis(null);
          setCurrentFlyerIndex(0);
          setFlyerEventSelections(new Map());
          setFlyerYearSelections(new Map());
          setUploading(false);
        });
      return;
    }

    setCurrentFlyerIndex(nextIndex);
    const nextFlyer = analysisResult.flyers[nextIndex];
    
    if (!nextFlyer || nextFlyer.clubNights.length === 0) {
      processNextFlyer(flyerId, analysisResult, nextIndex, eventSelections, yearSelections);
      return;
    }

    const nextEventName = nextFlyer.clubNights[0]?.eventName;
    const nextNeedsEventSelection = !nextEventName;
    
    const nextNeedsYearSelection = nextFlyer.clubNights.some(
      (cn) => !cn.date && cn.month && cn.day && cn.candidateYears.length > 1
    );

    if (nextNeedsEventSelection) {
      setShowEventSelection(true);
      setSuccessMessage(`Processing flyer ${nextIndex + 1} of ${totalFlyers}. Please select the event.`);
    } else if (nextNeedsYearSelection) {
      setShowYearSelection(true);
      setSuccessMessage(`Processing flyer ${nextIndex + 1} of ${totalFlyers}. Please select years for the dates.`);
    } else {
      processNextFlyer(flyerId, analysisResult, nextIndex, eventSelections, yearSelections);
    }
  };

  const handleEventSelectionConfirm = (eventId: number) => {
    if (!pendingAnalysis) return;
    
    const totalFlyers = pendingAnalysis.analysisResult.flyers?.length || 1;
    const currentFlyer = pendingAnalysis.analysisResult.flyers?.[currentFlyerIndex];
    
    if (!currentFlyer) return;

    const newEventSelections = new Map(flyerEventSelections);
    newEventSelections.set(currentFlyerIndex, eventId);
    setFlyerEventSelections(newEventSelections);
    setShowEventSelection(false);
    
    const needsYearSelection = currentFlyer.clubNights.some(
      (cn) => !cn.date && cn.month && cn.day && cn.candidateYears.length > 1
    );
    
    if (needsYearSelection) {
      setShowYearSelection(true);
      if (totalFlyers > 1) {
        setSuccessMessage(`Event selected for flyer ${currentFlyerIndex + 1} of ${totalFlyers}! Now please select years for the dates.`);
      } else {
        setSuccessMessage('Event selected! Now please select years for the dates.');
      }
    } else {
      if (totalFlyers > 1) {
        processNextFlyer(pendingAnalysis.flyerId, pendingAnalysis.analysisResult, currentFlyerIndex, newEventSelections, flyerYearSelections);
      } else {
        setUploading(true);
        completeUploadWithYears(pendingAnalysis.flyerId, pendingAnalysis.analysisResult, undefined, eventId)
          .finally(() => {
            setPendingAnalysis(null);
            setCurrentFlyerIndex(0);
            setFlyerEventSelections(new Map());
            setFlyerYearSelections(new Map());
            setUploading(false);
          });
      }
    }
  };

  const handleEventSelectionCancel = () => {
    setShowEventSelection(false);
    setPendingAnalysis(null);
    setCurrentFlyerIndex(0);
    setFlyerEventSelections(new Map());
    setFlyerYearSelections(new Map());
    setError('Upload cancelled. Please delete the flyer if needed from the Flyers page.');
  };

  const handleYearSelectionConfirm = async (selectedYearsMap: Map<string, number>) => {
    if (!pendingAnalysis) return;
    
    const totalFlyers = pendingAnalysis.analysisResult.flyers?.length || 1;
    
    const newYearSelections = new Map(flyerYearSelections);
    newYearSelections.set(currentFlyerIndex, selectedYearsMap);
    setFlyerYearSelections(newYearSelections);
    setShowYearSelection(false);
    
    if (totalFlyers > 1 && currentFlyerIndex < totalFlyers - 1) {
      processNextFlyer(
        pendingAnalysis.flyerId, 
        pendingAnalysis.analysisResult, 
        currentFlyerIndex, 
        flyerEventSelections, 
        newYearSelections
      );
    } else {
      setUploading(true);
      
      const mergedYearSelections = new Map<string, number>();
      newYearSelections.forEach((flyerSelections) => {
        flyerSelections.forEach((year, key) => {
          mergedYearSelections.set(key, year);
        });
      });
      
      const firstEventId = flyerEventSelections.size > 0 ? Array.from(flyerEventSelections.values())[0] : undefined;
      
      try {
        await completeUploadWithYears(
          pendingAnalysis.flyerId, 
          pendingAnalysis.analysisResult, 
          mergedYearSelections, 
          firstEventId || undefined
        );
      } finally {
        setPendingAnalysis(null);
        setCurrentFlyerIndex(0);
        setFlyerEventSelections(new Map());
        setFlyerYearSelections(new Map());
        setUploading(false);
      }
    }
  };

  const handleYearSelectionCancel = () => {
    setShowYearSelection(false);
    setPendingAnalysis(null);
    setCurrentFlyerIndex(0);
    setFlyerEventSelections(new Map());
    setFlyerYearSelections(new Map());
    setError('Upload cancelled. Please delete the flyer if needed from the Flyers page.');
  };

  return (
    <>
      {error && (
        <ErrorDiagnostics
          error={error}
          diagnostics={diagnostics}
          onClose={() => {
            setError(null);
            setDiagnostics(undefined);
          }}
        />
      )}
      
      {showPreview && pendingFlyerResults.length > 0 && (
        <FlyerPreviewModal
          flyerResults={pendingFlyerResults}
          onConfirm={handlePreviewConfirm}
          onCancel={handlePreviewCancel}
        />
      )}
      
      {showEventSelection && pendingAnalysis && (
        <EventSelectionModal
          onConfirm={handleEventSelectionConfirm}
          onCancel={handleEventSelectionCancel}
          currentFlyerIndex={currentFlyerIndex}
          totalFlyers={pendingAnalysis.analysisResult.flyers?.length || 1}
        />
      )}
      
      {showYearSelection && pendingAnalysis && (
        <YearSelectionModal
          clubNights={pendingAnalysis.analysisResult.flyers?.[currentFlyerIndex]?.clubNights || pendingAnalysis.analysisResult.clubNights}
          onConfirm={handleYearSelectionConfirm}
          onCancel={handleYearSelectionCancel}
          currentFlyerIndex={currentFlyerIndex}
          totalFlyers={pendingAnalysis.analysisResult.flyers?.length || 1}
        />
      )}
      
      <div className="card">
        <h2>Contribute to WasThere</h2>
        
        {successMessage && <div className="success-message" style={{ color: 'green', marginBottom: '1rem', padding: '1rem', backgroundColor: '#e8f5e9', borderRadius: '4px' }}>{successMessage}</div>}
        
        <div className="contribute-instructions" style={{ marginBottom: '2rem', backgroundColor: '#f5f5f5', padding: '1.5rem', borderRadius: '8px' }}>
          <h3>Upload Guidelines</h3>
          <p style={{ marginBottom: '1rem' }}>
            Help us preserve club culture history by uploading flyers and tickets from events between 1995-2005. 
            Our AI will automatically extract event details, dates, venues, and lineup information.
          </p>
          
          <h4 style={{ marginTop: '1.5rem', marginBottom: '0.5rem' }}>What to Upload:</h4>
          <ul style={{ marginLeft: '1.5rem', lineHeight: '1.8' }}>
            <li><strong>Tickets or flyers are both fine</strong> - as long as they include the venue, event name, date, and list of acts</li>
            <li><strong>Date requirements:</strong> We can cope without the year, but we must have the month and day</li>
          </ul>
          
          <h4 style={{ marginTop: '1.5rem', marginBottom: '0.5rem' }}>Best Practices for Quality:</h4>
          <ul style={{ marginLeft: '1.5rem', lineHeight: '1.8' }}>
            <li><strong>Ideal:</strong> The original digital design or high-resolution scan</li>
            <li><strong>Very good:</strong> A clean scan of a single flyer or ticket</li>
            <li><strong>Good:</strong> A clear photo of a single flyer or ticket</li>
            <li><strong>Acceptable:</strong> Multiple flyers/tickets in one image - we'll attempt to split them automatically
              <ul style={{ marginLeft: '1.5rem', marginTop: '0.5rem' }}>
                <li>Only show the <strong>detail side</strong> (avoid the picture/graphic side as it may confuse the AI)</li>
                <li>Keep them as <strong>separate</strong> and <strong>level</strong> as possible</li>
                <li>If the automatic split doesn't work well, cancel and try uploading them individually</li>
              </ul>
            </li>
          </ul>
          
          <h4 style={{ marginTop: '1.5rem', marginBottom: '0.5rem' }}>Preview and Confirmation:</h4>
          <ul style={{ marginLeft: '1.5rem', lineHeight: '1.8' }}>
            <li>You'll see a preview of extracted information before finalizing</li>
            <li>If it's mostly correct, proceed - you can edit details later</li>
            <li>It's better to have some record of an event, even if imperfect</li>
            <li>If the extraction is completely wrong (especially if multiple flyers are split incorrectly), cancel and try again with better images</li>
          </ul>
        </div>
        
        <div className="club-night-form">
          <h3>Upload Flyer or Ticket</h3>
          
          <form onSubmit={handleUpload}>
            <div className="form-group">
              <label htmlFor="flyer-file">Select Image File</label>
              <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                <input
                  id="flyer-file"
                  type="file"
                  accept="image/jpeg,image/jpg,image/png,image/gif,image/webp"
                  onChange={handleFileChange}
                  className="input"
                  style={{ flex: isMobile ? '1 1 100%' : '1' }}
                />
                {isMobile && (
                  <label 
                    htmlFor="flyer-camera"
                    className="btn btn-secondary"
                    style={{ 
                      display: 'inline-flex', 
                      alignItems: 'center', 
                      justifyContent: 'center',
                      padding: '0.5rem 1rem',
                      cursor: 'pointer',
                      whiteSpace: 'nowrap'
                    }}
                  >
                    📷 Use Camera
                    <input
                      id="flyer-camera"
                      type="file"
                      accept="image/jpeg,image/jpg,image/png,image/gif,image/webp"
                      capture="environment"
                      onChange={handleFileChange}
                      style={{ display: 'none' }}
                    />
                  </label>
                )}
              </div>
              {selectedFile && (
                <div style={{ marginTop: '0.5rem', fontSize: '0.9rem', color: '#666' }}>
                  Selected: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)
                </div>
              )}
            </div>

            <div className="form-group" style={{ marginTop: '1rem' }}>
              <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: 'pointer' }}>
                <input
                  type="checkbox"
                  checked={uploadingMultipleFlyers}
                  onChange={(e) => setUploadingMultipleFlyers(e.target.checked)}
                  style={{ cursor: 'pointer' }}
                />
                <span>Image contains multiple flyers (will attempt to split them automatically)</span>
              </label>
              <div style={{ marginTop: '0.5rem', fontSize: '0.85rem', color: '#666', marginLeft: '1.5rem' }}>
                ℹ️ Check this if your image shows multiple flyers/tickets. Leave unchecked for single flyers to avoid unwanted splitting.
              </div>
            </div>

            <div className="form-actions">
              <button 
                type="submit" 
                className={`btn btn-primary ${uploading ? 'btn-loading' : ''}`}
                disabled={uploading || !selectedFile}
              >
                {uploading && <span className="spinner"></span>}
                Upload and Analyze
              </button>
            </div>
            
            {uploading && (
              <div className="progress-text">
                Uploading and analyzing flyer... This may take a minute.
              </div>
            )}
          </form>
        </div>
      </div>
    </>
  );
}
