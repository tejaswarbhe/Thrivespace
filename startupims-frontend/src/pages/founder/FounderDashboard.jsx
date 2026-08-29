import { useEffect, useState } from 'react';
import api from '../../api/axiosInstance';
import { downloadPitchDeck } from '../../api/downloadPitchDeck';

export default function FounderDashboard() {
  const [startups, setStartups] = useState([]);
  const [applications, setApplications] = useState([]);
  const [reports, setReports] = useState([]);
  const [funding, setFunding] = useState([]);
  const [initialLoading, setInitialLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');

  const [milestones, setMilestones] = useState('');
  const [fundingAmount, setFundingAmount] = useState('');
  const [fundingType, setFundingType] = useState('Grant');
  const [pitchDeckFile, setPitchDeckFile] = useState(null);
  const [uploadingPitchDeck, setUploadingPitchDeck] = useState(false);
  const [downloadingPitchDeck, setDownloadingPitchDeck] = useState(false);

  async function initialLoad() {
    setInitialLoading(true);
    setError('');
    try {
      await refresh();
    } catch (err) {
      setError('Failed to load your data. Is the backend running?');
    } finally {
      setInitialLoading(false);
    }
  }

  async function refresh() {
    const [startupsRes, appsRes, reportsRes, fundingRes] = await Promise.all([
      api.get('/Startups'),
      api.get('/Applications'),
      api.get('/ProgressReports'),
      api.get('/Funding'),
    ]);
    setStartups(startupsRes.data);
    setApplications(appsRes.data);
    setReports(reportsRes.data);
    setFunding(fundingRes.data);
  }

  useEffect(() => {
    initialLoad();
  }, []);

  async function runAction(actionFn) {
    setActionError('');
    try {
      await actionFn();
      await refresh();
    } catch (err) {
      const detail = err.response?.data?.detail ?? err.response?.data ?? err.message;
      setActionError(`Action failed (${err.response?.status ?? 'network error'}): ${JSON.stringify(detail)}`);
    }
  }

  function submitApplication() {
    return runAction(() => api.post('/Applications', { remarks: 'Requesting review' }));
  }

  function submitFundingRequest(e) {
    e.preventDefault();
    if (!fundingAmount) return;
    runAction(() => api.post('/Funding', { amount: Number(fundingAmount), fundingType })).then(() => setFundingAmount(''));
  }
  function toggleTask(report) {
    return runAction(() => api.put(`/ProgressReports/${report.id}`, {
      isCompleted: !report.isCompleted,
      remarks: report.remarks ?? ''
    }));
  }

  async function uploadPitchDeck(e, startupId) {
    e.preventDefault();
    if (!pitchDeckFile) return;

    setActionError('');
    setUploadingPitchDeck(true);
    try {
      const formData = new FormData();
      formData.append('file', pitchDeckFile);
      // Do NOT set Content-Type manually - axios/the browser needs to add
      // the multipart boundary itself based on the FormData object.
      await api.post(`/Startups/${startupId}/pitch-deck`, formData);
      setPitchDeckFile(null);
      await refresh();
    } catch (err) {
      const detail = err.response?.data?.detail ?? err.response?.data ?? err.message;
      setActionError(`Upload failed (${err.response?.status ?? 'network error'}): ${JSON.stringify(detail)}`);
    } finally {
      setUploadingPitchDeck(false);
    }
  }

  async function handleDownloadPitchDeck(startupId, fileName) {
    setActionError('');
    setDownloadingPitchDeck(true);
    try {
      await downloadPitchDeck(startupId, fileName);
    } catch (err) {
      setActionError(`Download failed (${err.response?.status ?? 'network error'})`);
    } finally {
      setDownloadingPitchDeck(false);
    }
  }

  if (initialLoading) return <div className="container mt-4">Loading...</div>;

  const myStartup = startups[0]; // Founders only ever have one, per the 1:1 rule

  return (
    <div className="container mt-4 mb-5">
      <h2>Founder Dashboard</h2>
      {error && <div className="alert alert-danger">{error}</div>}
      {actionError && (
        <div className="alert alert-warning alert-dismissible">
          {actionError}
          <button type="button" className="btn-close" onClick={() => setActionError('')}></button>
        </div>
      )}

      <div className="card mb-4">
        <div className="card-body">
          <h5 className="card-title">Your Startup</h5>
          {myStartup ? (
            <>
              <p className="mb-1"><strong>{myStartup.name}</strong> ({myStartup.domain})</p>
              <p className="mb-1">{myStartup.description}</p>
              <span className="badge bg-primary text-white mt-2 mb-3">{myStartup.status}</span>

              <hr />
              <h6>Pitch Deck</h6>
              {myStartup.pitchDeckOriginalFileName ? (
                <p className="mb-2">
                  <strong>{myStartup.pitchDeckOriginalFileName}</strong>
                  <span className="text-muted"> — uploaded {new Date(myStartup.pitchDeckUploadedAt).toLocaleString()}</span>
                  <br />
                  <button
                    className="btn btn-sm btn-outline-primary mt-2"
                    onClick={() => handleDownloadPitchDeck(myStartup.id, myStartup.pitchDeckOriginalFileName)}
                    disabled={downloadingPitchDeck}
                  >
                    {downloadingPitchDeck ? 'Downloading...' : 'Download'}
                  </button>
                </p>
              ) : (
                <p className="text-muted mb-2">No pitch deck uploaded yet.</p>
              )}

              <form onSubmit={(e) => uploadPitchDeck(e, myStartup.id)} className="d-flex gap-2 align-items-center">
                <input
                  type="file"
                  className="form-control form-control-sm"
                  accept=".pdf,.pptx"
                  onChange={(e) => setPitchDeckFile(e.target.files[0] ?? null)}
                />
                <button className="btn btn-sm btn-primary" type="submit" disabled={!pitchDeckFile || uploadingPitchDeck}>
                  {uploadingPitchDeck ? 'Uploading...' : (myStartup.pitchDeckOriginalFileName ? 'Replace' : 'Upload')}
                </button>
              </form>
              <div className="form-text">PDF or PPTX, up to 20 MB.</div>
            </>
          ) : (
            <p className="text-muted">No startup found on your account yet.</p>
          )}
        </div>
      </div>

      <div className="row">
        <div className="col-md-4 mb-4">
          <div className="card h-100">
            <div className="card-body">
              <h5 className="card-title">Incubation Applications</h5>
              {(() => {
                const hasActiveApp = applications.some(a => ['Submitted', 'UnderReview'].includes(a.status));
                const isAccepted = applications.some(a => a.status === 'Accepted');
                
                if (isAccepted) {
                  return <p className="text-success fw-bold mt-2">Application Accepted</p>;
                } else if (!hasActiveApp) {
                  return (
                    <button className="btn btn-sm btn-primary mb-3" onClick={submitApplication}>
                      {applications.some(a => a.status === 'Rejected') ? "Submit New Application" : "Submit Application"}
                    </button>
                  );
                }
                return null;
              })()}
              <ul className="list-group list-group-flush mt-3">
                {applications.map((a) => (
                  <li key={a.id} className="list-group-item d-flex justify-content-between">
                    {new Date(a.submissionDate).toLocaleString()}
                    <span className="badge bg-secondary">{a.status}</span>
                  </li>
                ))}
                {applications.length === 0 && <li className="list-group-item text-muted">None yet</li>}
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-4 mb-4">
          <div className="card h-100">
            <div className="card-body">
              <h5 className="card-title">Tasks from Your Mentor</h5>
              <ul className="list-group list-group-flush mt-3">
                {reports.map((r) => (
                  <li key={r.id} className="list-group-item d-flex align-items-center gap-2">
                    <input
                      type="checkbox"
                      className="form-check-input"
                      checked={r.isCompleted}
                      onChange={() => toggleTask(r)}
                    />
                    <span className={r.isCompleted ? 'text-decoration-line-through text-muted' : ''}>{r.milestones}</span>
                  </li>
                ))}
                {reports.length === 0 && <li className="list-group-item text-muted">No tasks yet</li>}
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-4 mb-4">
          <div className="card h-100">
            <div className="card-body">
              <h5 className="card-title">Funding Requests</h5>
              <form onSubmit={submitFundingRequest} className="mb-3">
                <input
                  type="number"
                  className="form-control mb-2"
                  placeholder="Amount"
                  value={fundingAmount}
                  onChange={(e) => setFundingAmount(e.target.value)}
                />
                <select className="form-select mb-2" value={fundingType} onChange={(e) => setFundingType(e.target.value)}>
                  <option>Grant</option>
                  <option>Loan</option>
                  <option>Equity</option>
                </select>
                <button className="btn btn-sm btn-primary" type="submit">Request Funding</button>
              </form>
              <ul className="list-group list-group-flush mt-3">
                {funding.map((f) => (
                  <li key={f.id} className="list-group-item d-flex justify-content-between align-items-center">
                    <span>${f.amount} ({f.fundingType})</span>
                    <span className="d-flex align-items-center gap-2">
                      <span className="badge bg-secondary">{f.approvalStatus}</span>
                      {f.approvalStatus === 'Approved' && (
                        <span className="badge bg-info text-dark">Payment: {f.paymentStatus ?? 'PENDING'}</span>
                      )}
                    </span>
                  </li>
                ))}
                {funding.length === 0 && <li className="list-group-item text-muted">None yet</li>}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
