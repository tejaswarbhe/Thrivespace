import { useEffect, useState } from 'react';
import api from '../../api/axiosInstance';
import { downloadPitchDeck } from '../../api/downloadPitchDeck';

export default function AdminDashboard() {
  const [startups, setStartups] = useState([]);
  const [applications, setApplications] = useState([]);
  const [funding, setFunding] = useState([]);
  const [mentors, setMentors] = useState([]);
  const [assignments, setAssignments] = useState([]);
  const [initialLoading, setInitialLoading] = useState(true);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [downloadingId, setDownloadingId] = useState(null);
  const [processingFundingId, setProcessingFundingId] = useState(null);

  const [assignStartupId, setAssignStartupId] = useState('');
  const [assignMentorId, setAssignMentorId] = useState('');

  async function handleDownload(startupId, fileName) {
    setDownloadingId(startupId);
    try {
      await downloadPitchDeck(startupId, fileName);
    } catch (err) {
      setActionError(`Download failed (${err.response?.status ?? 'network error'})`);
    } finally {
      setDownloadingId(null);
    }
  }

  // Called on first mount only - shows the full-page "Loading..." state
  async function initialLoad() {
    setInitialLoading(true);
    setError('');
    try {
      await refresh();
    } catch (err) {
      setError('Failed to load admin data. Is the backend running?');
    } finally {
      setInitialLoading(false);
    }
  }

  // Called after every action - refetches data WITHOUT blanking the page,
  // so clicking Approve/Accept doesn't look like a reload.
  async function refresh() {
    const [startupsRes, appsRes, fundingRes, mentorsRes, assignmentsRes] = await Promise.all([
      api.get('/Startups'),
      api.get('/Applications'),
      api.get('/Funding'),
      api.get('/Mentors'),
      api.get('/Mentors/assignments'),
    ]);
    setStartups(startupsRes.data);
    setApplications(appsRes.data);
    setFunding(fundingRes.data);
    setMentors(mentorsRes.data);
    setAssignments(assignmentsRes.data);
  }

  useEffect(() => {
    initialLoad();
  }, []);

  // Wraps every mutating action: clears old error, runs the action, refreshes
  // data silently, and - critically - actually shows an error if it fails,
  // instead of failing silently like before.
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

  function updateApplicationStatus(id, status) {
    return runAction(() => api.put(`/Applications/${id}/status`, { status }));
  }

  function updateFundingApproval(id, approvalStatus) {
    setActionError('');
    setProcessingFundingId(id);
    return api.put(`/Funding/${id}/approval`, { approvalStatus })
      .then(res => {
        if (approvalStatus === 'Approved' && res.data.paymentUrl) {
          window.location.href = res.data.paymentUrl;
        } else {
          setProcessingFundingId(null);
          refresh();
        }
      })
      .catch(err => {
        setProcessingFundingId(null);
        const detail = err.response?.data?.detail ?? err.response?.data ?? err.message;
        setActionError(`Action failed (${err.response?.status ?? 'network error'}): ${JSON.stringify(detail)}`);
      });
  }

  function assignMentor(e) {
    e.preventDefault();
    if (!assignStartupId || !assignMentorId) return;
    runAction(() => api.post(`/Mentors/assign?startupId=${assignStartupId}&mentorId=${assignMentorId}`)).then(() => {
      setAssignStartupId('');
      setAssignMentorId('');
    });
  }

  function unassignMentor(assignmentId) {
    return runAction(() => api.delete(`/Mentors/assignments/${assignmentId}`));
  }

  const assignedStartupIds = new Set(assignments.map((a) => a.startupId));
  const unassignedStartups = startups.filter((s) => !assignedStartupIds.has(s.id));

  if (initialLoading) return <div className="container mt-4">Loading...</div>;

  return (
    <div className="container mt-4 mb-5">
      <h2>Admin Dashboard</h2>
      {error && <div className="alert alert-danger">{error}</div>}
      {actionError && (
        <div className="alert alert-warning alert-dismissible">
          {actionError}
          <button type="button" className="btn-close" onClick={() => setActionError('')}></button>
        </div>
      )}

      <div className="card mb-4">
        <div className="card-body">
          <h5 className="card-title">All Startups ({startups.length})</h5>
          <table className="table align-middle mt-2">
            <thead><tr><th>Name</th><th>Domain</th><th>Status</th><th>Pitch Deck</th></tr></thead>
            <tbody>
              {startups.map((s) => (
                <tr key={s.id}>
                  <td>{s.name}</td>
                  <td>{s.domain}</td>
                  <td>{s.status}</td>
                  <td>
                    {s.pitchDeckOriginalFileName ? (
                      <button
                        className="btn btn-sm btn-outline-primary"
                        onClick={() => handleDownload(s.id, s.pitchDeckOriginalFileName)}
                        disabled={downloadingId === s.id}
                      >
                        {downloadingId === s.id ? 'Downloading...' : 'Download'}
                      </button>
                    ) : (
                      <span className="text-muted">None</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card mb-4">
        <div className="card-body">
          <h5 className="card-title">Assign a Mentor</h5>
          <form onSubmit={assignMentor} className="row g-2 align-items-end mb-3">
            <div className="col-auto">
              <label className="form-label">Startup</label>
              <select className="form-select" value={assignStartupId} onChange={(e) => setAssignStartupId(e.target.value)}>
                <option value="">Select...</option>
                {unassignedStartups.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
              {unassignedStartups.length === 0 && startups.length > 0 && (
                <div className="form-text">Every startup already has an active mentor assigned.</div>
              )}
            </div>
            <div className="col-auto">
              <label className="form-label">Mentor</label>
              <select className="form-select" value={assignMentorId} onChange={(e) => setAssignMentorId(e.target.value)}>
                <option value="">Select...</option>
                {mentors.map((m) => <option key={m.id} value={m.id}>{m.name} - {m.expertise}</option>)}
              </select>
            </div>
            <div className="col-auto">
              <button className="btn btn-primary" type="submit" disabled={unassignedStartups.length === 0}>Assign</button>
            </div>
          </form>

          <h6>Current Assignments</h6>
          <ul className="list-group list-group-flush mt-2">
            {assignments.map((a) => {
              const startup = startups.find((s) => s.id === a.startupId);
              const mentor = mentors.find((m) => m.id === a.mentorId);
              return (
                <li key={a.id} className="list-group-item d-flex justify-content-between align-items-center">
                  <span>{startup?.name ?? `Startup #${a.startupId}`} &rarr; {mentor?.name ?? `Mentor #${a.mentorId}`} ({mentor?.expertise ?? "No Expertise"})</span>
                  <button className="btn btn-sm btn-outline-secondary" onClick={() => unassignMentor(a.id)}>Unassign</button>
                </li>
              );
            })}
            {assignments.length === 0 && <li className="list-group-item text-muted">No active assignments</li>}
          </ul>
        </div>
      </div>

      <div className="row">
        <div className="col-md-6 mb-4">
          <div className="card h-100">
            <div className="card-body">
              <h5 className="card-title">Incubation Applications</h5>
              <ul className="list-group list-group-flush mt-3">
                {applications.map((a) => {
                  const startup = startups.find(s => s.id === a.startupId);
                  return (
                  <li key={a.id} className="list-group-item d-flex justify-content-between align-items-center">
                    <span>{startup ? startup.name : `Startup #${a.startupId}`} (App #{a.id}) - {a.status}</span>
                    {a.status === 'Submitted' && (
                      <div className="btn-group btn-group-sm">
                        <button className="btn btn-outline-success" onClick={() => updateApplicationStatus(a.id, 'Accepted')}>Accept</button>
                        <button className="btn btn-outline-danger" onClick={() => updateApplicationStatus(a.id, 'Rejected')}>Reject</button>
                      </div>
                    )}
                  </li>
                )})}
                {applications.length === 0 && <li className="list-group-item text-muted">None</li>}
              </ul>
            </div>
          </div>
        </div>

        <div className="col-md-6 mb-4">
          <div className="card h-100">
            <div className="card-body">
              <h5 className="card-title">Funding Requests</h5>
              <ul className="list-group list-group-flush mt-3">
                {funding.map((f) => {
                  const startup = startups.find(s => s.id === f.startupId);
                  return (
                  <li key={f.id} className="list-group-item d-flex justify-content-between align-items-center">
                    <div>
                      <strong>{startup ? startup.name : `Startup #${f.startupId}`}</strong><br/>
                      ${f.amount} ({f.fundingType})<br/>
                      Approval: {f.approvalStatus}
                      {f.approvalStatus === 'Approved' && <><br/>Payment: {f.paymentStatus ?? 'NULL'}</>}
                    </div>
                    {f.approvalStatus === 'Pending' && (
                      <div className="btn-group btn-group-sm">
                        <button className="btn btn-outline-success" disabled={processingFundingId === f.id} onClick={() => updateFundingApproval(f.id, 'Approved')}>
                          {processingFundingId === f.id ? 'Processing payment...' : 'Approve'}
                        </button>
                        <button className="btn btn-outline-danger" disabled={processingFundingId === f.id} onClick={() => updateFundingApproval(f.id, 'Rejected')}>Reject</button>
                      </div>
                    )}
                  </li>
                )})}
                {funding.length === 0 && <li className="list-group-item text-muted">None</li>}
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
