import { useState, useEffect } from 'react';
import type { Flyer, DiagnosticInfo, FlyerAnalysisResult, Event, Venue } from '../types';
import { flyersApi, eventsApi, venuesApi, type AutoPopulateResult, type YearSelection } from '../services/api';
import { ErrorDiagnostics } from './ErrorDiagnostics';
import { YearSelectionModal } from './YearSelectionModal';
import { EventSelectionModal } from './EventSelectionModal';
import { usePagination } from '../hooks/usePagination';
import { Pagination } from './Pagination';

export function FlyerList() {
  const [flyers, setFlyers] = useState<Flyer[]>([]);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [diagnostics, setDiagnostics] = useState<DiagnosticInfo | undefined>(undefined);
  const [autoPopulating, setAutoPopulating] = useState<number | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [showEventSelection, setShowEventSelection] = useState(false);
  const [showYearSelection, setShowYearSelection] = useState(false);
  const [pendingAnalysis, setPendingAnalysis] = useState<{ flyerId: number; analysisResult: FlyerAnalysisResult; needsEventSelection: boolean } | null>(null);
  const [selectedEventId, setSelectedEventId] = useState<number | null>(null);
  const [currentFlyerIndex, setCurrentFlyerIndex] = useState<number>(0);
  const [flyerEventSelections, setFlyerEventSelections] = useState<Map<number, number>>(new Map());
  const [flyerYearSelections, setFlyerYearSelections] = useState<Map<number, Map<string, number>>>(new Map());

  // Filter and sort state
  const [showFilters, setShowFilters] = useState(false);
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [filterEventId, setFilterEventId] = useState<number>(0);
  const [filterVenueId, setFilterVenueId] = useState<number>(0);
  const [filterDateFrom, setFilterDateFrom] = useState<string>('');
  const [filterDateTo, setFilterDateTo] = useState<string>('');

  // Get all events and venues for filtering
  const [events, setEvents] = useState<Event[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);

  // Apply filters and sorting to flyers
  const filteredAndSortedFlyers = flyers
    .filter(flyer => {
      if (filterEventId > 0 && flyer.eventId !== filterEventId) return false;
      if (filterVenueId > 0 && flyer.venueId !== filterVenueId) return false;
      if (filterDateFrom && new Date(flyer.earliestClubNightDate) < new Date(filterDateFrom)) return false;
      if (filterDateTo && new Date(flyer.earliestClubNightDate) > new Date(filterDateTo)) return false;
      return true;
    })
    .sort((a, b) => {
      const comparison = new Date(a.earliestClubNightDate).getTime() - new Date(b.earliestClubNightDate).getTime();
      return sortOrder === 'asc' ? comparison : -comparison;
    });

  const {
    currentPage,
    pageSize,
    paginatedItems,
    totalPages,
    totalItems,
    setPage,
    setPageSize,
  } = usePagination({ items: filteredAndSortedFlyers, initialPageSize: 12 });

  useEffect(() => {
    loadFlyers();
    loadFiltersData();
  }, []);

  const loadFiltersData = async () => {
    try {
      const [eventsData, venuesData] = await Promise.all([
        eventsApi.getAll(),
        venuesApi.getAll(),
      ]);
      setEvents(eventsData);
      setVenues(venuesData);
    } catch (error) {
      console.error('Failed to load filter data:', error);
    }
  };

  const loadFlyers = async () => {
    try {
      const data = await flyersApi.getAll();
      setFlyers(data);
    } catch (error) {
      console.error('Failed to load flyers:', error);
      setError('Failed to load flyers');
    }
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
      const result = await flyersApi.upload(selectedFile);
      
      if (result.success && result.flyer && result.analysisResult) {
        const needsEventSelection = result.needsEventSelection || false;
        
        // Ensure flyers array exists (backward compatibility)
        if (!result.analysisResult.flyers || result.analysisResult.flyers.length === 0) {
          // If old format, convert to new format
          if (result.analysisResult.clubNights && result.analysisResult.clubNights.length > 0) {
            result.analysisResult.flyers = [{ clubNights: result.analysisResult.clubNights }];
          } else {
            setError('No flyer data found in analysis');
            setUploading(false);
            return;
          }
        }
        
        // Store pending analysis for wizard flow
        setPendingAnalysis({
          flyerId: result.flyer.id,
          analysisResult: result.analysisResult,
          needsEventSelection: needsEventSelection
        });

        // Initialize wizard state
        setCurrentFlyerIndex(0);
        setFlyerEventSelections(new Map());
        setFlyerYearSelections(new Map());

        const totalFlyers = result.analysisResult.flyers?.length || 1;
        const firstFlyer = result.analysisResult.flyers?.[0];
        
        if (!firstFlyer || firstFlyer.clubNights.length === 0) {
          setError('No flyer data found in analysis');
          setUploading(false);
          return;
        }

        // Check if first flyer needs event selection
        const firstEventName = firstFlyer.clubNights[0]?.eventName;
        const firstNeedsEventSelection = !firstEventName;
        
        // Check if first flyer needs year selection
        const firstNeedsYearSelection = firstFlyer.clubNights.some(
          (cn) => !cn.date && cn.month && cn.day && cn.candidateYears.length > 1
        );

        // Build appropriate message
        let message = 'Flyer uploaded and analyzed!';
        if (totalFlyers > 1) {
          message += ` Found ${totalFlyers} flyers in the image.`;
        }

        // Start wizard flow for the first flyer
        if (firstNeedsEventSelection) {
          setShowEventSelection(true);
          setSuccessMessage(message + ' Please select the event.');
        } else if (firstNeedsYearSelection) {
          setShowYearSelection(true);
          setSuccessMessage(message + ' Please select years for the dates.');
        } else if (totalFlyers > 1) {
          // First flyer doesn't need input but there are more flyers
          // Process next flyer or complete if all are done
          processNextFlyer(result.flyer.id, result.analysisResult, 0, new Map(), new Map());
        } else {
          // Single flyer, no user input needed
          await completeUploadWithYears(result.flyer.id, result.analysisResult);
        }
        
        // Reset form
        setSelectedFile(null);
        
        // Reset file input
        const fileInput = document.getElementById('flyer-file') as HTMLInputElement;
        if (fileInput) fileInput.value = '';
      } else {
        setError(result.message || 'Failed to upload flyer');
        setDiagnostics(result.diagnostics);
      }
    } catch (error) {
      console.error('Failed to upload flyer:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to upload flyer';
      setError(errorMessage);
      setDiagnostics(undefined);
    } finally {
      setUploading(false);
    }
  };

  const completeUploadWithYears = async (flyerId: number, analysisResult: FlyerAnalysisResult, selectedYearsMap?: Map<string, number>, eventId?: number) => {
    try {
      // Build the year selections array
      const selectedYears: YearSelection[] = [];
      
      for (const cn of analysisResult.clubNights) {
        if (!cn.date && cn.month && cn.day) {
          const key = `${cn.month}-${cn.day}`;
          let year: number;
          
          if (selectedYearsMap && selectedYearsMap.has(key)) {
            year = selectedYearsMap.get(key)!;
          } else if (cn.candidateYears.length > 0) {
            // Default to first candidate if not explicitly selected
            year = cn.candidateYears[0];
          } else {
            continue; // Skip if no candidate years
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
        setSuccessMessage(`Flyer processed! ${result.message}`);
        await loadFlyers();
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
      // All flyers processed, complete the upload
      setUploading(true);
      
      // Merge all year selections from all flyers into one map
      const mergedYearSelections = new Map<string, number>();
      yearSelections.forEach((flyerSelections) => {
        flyerSelections.forEach((year, key) => {
          mergedYearSelections.set(key, year);
        });
      });
      
      // NOTE: Current limitation - we use the first event selection for all club nights
      // because the Flyer database model only supports one EventId per image upload.
      // If multiple different events are detected in one image, users should upload them separately.
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

    // Process next flyer
    setCurrentFlyerIndex(nextIndex);
    const nextFlyer = analysisResult.flyers[nextIndex];
    
    if (!nextFlyer || nextFlyer.clubNights.length === 0) {
      // Skip this flyer and move to next
      processNextFlyer(flyerId, analysisResult, nextIndex, eventSelections, yearSelections);
      return;
    }

    // Check if next flyer needs event selection
    const nextEventName = nextFlyer.clubNights[0]?.eventName;
    const nextNeedsEventSelection = !nextEventName;
    
    // Check if next flyer needs year selection
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
      // No input needed for this flyer, move to next
      processNextFlyer(flyerId, analysisResult, nextIndex, eventSelections, yearSelections);
    }
  };

  const handleEventSelectionConfirm = (eventId: number) => {
    if (!pendingAnalysis) return;
    
    const totalFlyers = pendingAnalysis.analysisResult.flyers?.length || 1;
    const currentFlyer = pendingAnalysis.analysisResult.flyers?.[currentFlyerIndex];
    
    if (!currentFlyer) return;

    // Store the selected event ID for this flyer
    const newEventSelections = new Map(flyerEventSelections);
    newEventSelections.set(currentFlyerIndex, eventId);
    setFlyerEventSelections(newEventSelections);
    setShowEventSelection(false);
    
    // Check if current flyer needs year selection
    const needsYearSelection = currentFlyer.clubNights.some(
      (cn) => !cn.date && cn.month && cn.day && cn.candidateYears.length > 1
    );
    
    if (needsYearSelection) {
      // Proceed to year selection for current flyer
      setShowYearSelection(true);
      if (totalFlyers > 1) {
        setSuccessMessage(`Event selected for flyer ${currentFlyerIndex + 1} of ${totalFlyers}! Now please select years for the dates.`);
      } else {
        setSuccessMessage('Event selected! Now please select years for the dates.');
      }
    } else {
      // No year selection needed for this flyer, move to next or complete
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
    setError('Upload cancelled. Please delete the flyer if needed.');
  };

  const handleYearSelectionConfirm = async (selectedYearsMap: Map<string, number>) => {
    if (!pendingAnalysis) return;
    
    const totalFlyers = pendingAnalysis.analysisResult.flyers?.length || 1;
    
    // Store the year selections for this flyer
    const newYearSelections = new Map(flyerYearSelections);
    newYearSelections.set(currentFlyerIndex, selectedYearsMap);
    setFlyerYearSelections(newYearSelections);
    setShowYearSelection(false);
    
    // If there are more flyers to process, continue the wizard
    if (totalFlyers > 1 && currentFlyerIndex < totalFlyers - 1) {
      processNextFlyer(
        pendingAnalysis.flyerId, 
        pendingAnalysis.analysisResult, 
        currentFlyerIndex, 
        flyerEventSelections, 
        newYearSelections
      );
    } else {
      // All flyers processed or single flyer, complete upload
      setUploading(true);
      
      // Merge all year selections from all flyers
      const mergedYearSelections = new Map<string, number>();
      newYearSelections.forEach((flyerSelections) => {
        flyerSelections.forEach((year, key) => {
          mergedYearSelections.set(key, year);
        });
      });
      
      // NOTE: Current limitation - we use the first event selection for all club nights
      // because the Flyer database model only supports one EventId per image upload.
      // If multiple different events are detected in one image, users should upload them separately.
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
    setError('Upload cancelled. Please delete the flyer if needed.');
  };

  const handleDelete = async (id: number) => {
    if (!confirm('Are you sure you want to delete this flyer?')) return;

    try {
      await flyersApi.delete(id);
      await loadFlyers();
    } catch (error) {
      console.error('Failed to delete flyer:', error);
      setError('Failed to delete flyer');
    }
  };

  const handleAutoPopulate = async (id: number) => {
    setAutoPopulating(id);
    setError(null);
    setDiagnostics(undefined);
    setSuccessMessage(null);

    try {
      const result: AutoPopulateResult = await flyersApi.autoPopulate(id);
      
      if (result.success) {
        setSuccessMessage(result.message);
        // Reload flyers to show updated club nights
        await loadFlyers();
        
        // Clear success message after 5 seconds
        setTimeout(() => setSuccessMessage(null), 5000);
      } else {
        setError(result.message || 'Failed to auto-populate from flyer');
        setDiagnostics(result.diagnostics);
      }
    } catch (error) {
      console.error('Failed to auto-populate:', error);
      const errorMessage = error instanceof Error ? error.message : 'Failed to auto-populate from flyer';
      setError(errorMessage);
      setDiagnostics(undefined);
    } finally {
      setAutoPopulating(null);
    }
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
        <h2>Flyers</h2>
        
        <div className="club-night-form">
          <h3>Upload New Flyer</h3>
          {successMessage && <div className="success-message" style={{ color: 'green', marginBottom: '1rem' }}>{successMessage}</div>}
        
        <form onSubmit={handleUpload}>
          <div className="form-group">
            <label htmlFor="flyer-file">Select Image File</label>
            <input
              id="flyer-file"
              type="file"
              accept="image/jpeg,image/jpg,image/png,image/gif,image/webp"
              onChange={handleFileChange}
              className="input"
            />
            {selectedFile && (
              <div style={{ marginTop: '0.5rem', fontSize: '0.9rem', color: '#666' }}>
                Selected: {selectedFile.name} ({(selectedFile.size / 1024 / 1024).toFixed(2)} MB)
              </div>
            )}
          </div>

          <div className="form-actions">
            <button 
              type="submit" 
              className={`btn btn-primary ${uploading ? 'btn-loading' : ''}`}
              disabled={uploading || !selectedFile}
            >
              {uploading && <span className="spinner"></span>}
              Upload and Analyze Flyer
            </button>
          </div>
          
          {uploading && (
            <div className="progress-text">
              Uploading and analyzing flyer... This may take a minute.
            </div>
          )}
          
          <div style={{ marginTop: '1rem', fontSize: '0.9rem', color: '#666' }}>
            <p>The flyer will be automatically analyzed to extract:</p>
            <ul style={{ marginLeft: '1.5rem', marginTop: '0.5rem' }}>
              <li>Event and venue information</li>
              <li>Event dates (with automatic year inference)</li>
              <li>Performing acts/DJs</li>
            </ul>
          </div>
        </form>
      </div>

      <div className="filter-sort-controls">
        <button 
          className="btn btn-small"
          onClick={() => setShowFilters(!showFilters)}
        >
          {showFilters ? '▼' : '▶'} Filters
        </button>
        
        <div className="sort-control">
          <label>Sort by date:</label>
          <select
            value={sortOrder}
            onChange={(e) => setSortOrder(e.target.value as 'asc' | 'desc')}
            className="input"
          >
            <option value="desc">Newest first</option>
            <option value="asc">Oldest first</option>
          </select>
        </div>
      </div>

      {showFilters && (
        <div className="filter-panel">
          <div className="filter-panel-header">
            <h4>Filter Flyers</h4>
            <button 
              className="btn btn-small"
              onClick={() => {
                setFilterEventId(0);
                setFilterVenueId(0);
                setFilterDateFrom('');
                setFilterDateTo('');
              }}
            >
              Clear All
            </button>
          </div>
          
          <div className="filter-grid">
            <div className="filter-group">
              <label>Event</label>
              <select
                value={filterEventId}
                onChange={(e) => setFilterEventId(Number(e.target.value))}
                className="input"
              >
                <option value={0}>All events</option>
                {events.map((event) => (
                  <option key={event.id} value={event.id}>
                    {event.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label>Venue</label>
              <select
                value={filterVenueId}
                onChange={(e) => setFilterVenueId(Number(e.target.value))}
                className="input"
              >
                <option value={0}>All venues</option>
                {venues.map((venue) => (
                  <option key={venue.id} value={venue.id}>
                    {venue.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="filter-group">
              <label>Date From</label>
              <input
                type="date"
                value={filterDateFrom}
                onChange={(e) => setFilterDateFrom(e.target.value)}
                className="input"
              />
            </div>

            <div className="filter-group">
              <label>Date To</label>
              <input
                type="date"
                value={filterDateTo}
                onChange={(e) => setFilterDateTo(e.target.value)}
                className="input"
              />
            </div>
          </div>
        </div>
      )}

      <div className="flyers-grid">
        {filteredAndSortedFlyers.length === 0 ? (
          <div className="empty-state">
            {flyers.length === 0 
              ? 'No flyers uploaded yet.' 
              : 'No flyers match the current filters.'}
          </div>
        ) : (
          paginatedItems.map((flyer) => (
            <div key={flyer.id} className="flyer-card">
              <div className="flyer-image-container">
                <img
                  src={flyersApi.getImageUrl(flyer.filePath)}
                  alt={flyer.fileName}
                  className="flyer-image"
                />
              </div>
              <div className="flyer-info">
                <div className="flyer-details">
                  <div><strong>Event:</strong> {flyer.event?.name || 'N/A'}</div>
                  <div><strong>Venue:</strong> {flyer.venue?.name || 'N/A'}</div>
                  <div><strong>Date:</strong> {new Date(flyer.earliestClubNightDate).toLocaleDateString()}</div>
                  <div className="flyer-filename">{flyer.fileName}</div>
                </div>
                <div style={{ display: 'flex', gap: '0.5rem' }}>
                  <button 
                    onClick={() => handleAutoPopulate(flyer.id)} 
                    className="btn btn-small btn-primary"
                    disabled={autoPopulating === flyer.id}
                    title="Re-analyze flyer to extract additional information"
                  >
                    {autoPopulating === flyer.id ? 'Analyzing...' : 'Analyze'}
                  </button>
                  <button 
                    onClick={() => handleDelete(flyer.id)} 
                    className="btn btn-small btn-danger"
                  >
                    Delete
                  </button>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {filteredAndSortedFlyers.length > 0 && (
        <Pagination
          currentPage={currentPage}
          totalPages={totalPages}
          pageSize={pageSize}
          totalItems={totalItems}
          onPageChange={setPage}
          onPageSizeChange={setPageSize}
          pageSizeOptions={[12, 24, 48, 96]}
        />
      )}
    </div>
    </>
  );
}
