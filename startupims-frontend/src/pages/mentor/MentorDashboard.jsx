import { useEffect, useState } from 'react';
import api from '../../api/axiosInstance';
import { downloadPitchDeck } from '../../api/downloadPitchDeck';

export default function MentorDashboard() {
  const [startups, setStartups] = useState([]);
  const [reports, setReports] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [taskText, setTaskText] = useState({}); // { [startupId]: text }
  const [downloadingId, setDownloadingId] = useState(null);

  async function handleDownload(startupId, fileName) {
    setDownloadingId(startupId);
    try {
      await downloadPitchDeck(startupId, fileName);
    } catch (err) {
      setError(`Download failed (${err.response?.status ?? 'network error'})`);
    } finally {
      setDownloadingId(null);
    }
  }

  async function loadAll() {
    setLoading(true);
    setError('');
    try {
      const [startupsRes, reportsRes] = await Promise.all([
        api.get('/Startups'),
        api.get('/ProgressReports'),
      ]);
      setStartups(startupsRes.data);
      setReports(reportsRes.data);
    } catch (err) {
      setError('Failed to load your assigned startups.');
    } finally {
      setLoading(false);
    }
  }

  function assignTask(startupId) {
    const text = (taskText[startupId] || '').trim();
    if (!text) return;
    api.post('/ProgressReports', { startupId, milestones: text }).then(() => {
      setTaskText((prev) => ({ ...prev, [startupId]: '' }));
      loadAll();
    });
  }

  useEffect(() => {
    loadAll();
  }, []);

  if (loading) return <div className="container mt-4">Loading...</div>;

  return (
    <div className="container mt-4 mb-5">
      <h2>Mentor Dashboard</h2>
      <p className="text-muted">Startups you're currently assigned to mentor.</p>
      {error && <div className="alert alert-danger">{error}</div>}

      {startups.length === 0 && (
        <div className="alert alert-info">
          No startups assigned to you yet - an Admin needs to assign one.
        </div>
      )}

      <div className="row">
        {startups.map((s) => (
          <div className="col-md-6 mb-4" key={s.id}>
            <div className="card h-100">
              <div className="card-body">
                <h5 className="card-title">{s.name}</h5>
                <h6 className="card-subtitle mb-2 text-muted">{s.domain}</h6>
                <p className="card-text">{s.description}</p>
                <span className="badge bg-primary text-white mb-3 mt-1">{s.status}</span>

                {s.pitchDeckOriginalFileName && (
                  <div className="mb-3">
                    <button
                      className="btn btn-sm btn-outline-primary"
                      onClick={() => handleDownload(s.id, s.pitchDeckOriginalFileName)}
                      disabled={downloadingId === s.id}
                    >
                      {downloadingId === s.id ? 'Downloading...' : `Download Pitch Deck (${s.pitchDeckOriginalFileName})`}
                    </button>
                  </div>
                )}

                <h6>Tasks</h6>
                <form className="d-flex gap-2 mb-2" onSubmit={(e) => { e.preventDefault(); assignTask(s.id); }}>
                  <input
                    className="form-control form-control-sm"
                    placeholder="New task for this startup..."
                    value={taskText[s.id] || ''}
                    onChange={(e) => setTaskText((prev) => ({ ...prev, [s.id]: e.target.value }))}
                  />
                  <button className="btn btn-sm btn-primary" type="submit">Add</button>
                </form>
                <ul className="list-group list-group-flush mt-2">
                  {reports.filter((r) => r.startupId === s.id).map((r) => (
                    <li key={r.id} className="list-group-item d-flex justify-content-between align-items-center">
                      <span className={r.isCompleted ? 'text-decoration-line-through text-muted' : ''}>{r.milestones}</span>
                      <span className={`badge ${r.isCompleted ? 'bg-success' : 'bg-secondary'}`}>
                        {r.isCompleted ? 'Done' : 'Pending'}
                      </span>
                    </li>
                  ))}
                  {reports.filter((r) => r.startupId === s.id).length === 0 && (
                    <li className="list-group-item text-muted">No tasks yet</li>
                  )}
                </ul>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
