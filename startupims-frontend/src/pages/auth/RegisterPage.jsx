import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';

export default function RegisterPage() {
  const [form, setForm] = useState({
    name: '',
    email: '',
    password: '',
    role: 'Founder',
    startupName: '',
    startupDomain: '',
    startupDescription: '',
    startupFoundingDate: '',
    mentorExpertise: '',
    mentorExperienceYears: '',
    mentorOrganization: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  function update(field, value) {
    setForm((prev) => ({ ...prev, [field]: value }));
  }

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    setLoading(true);

    // Only send the fields relevant to the chosen role - matches
    // RegisterRequest on the backend, which ignores the irrelevant ones anyway,
    // but keeps the request body clean.
    const payload = {
      name: form.name,
      email: form.email,
      password: form.password,
      role: form.role,
      ...(form.role === 'Founder' && {
        startupName: form.startupName,
        startupDomain: form.startupDomain,
        startupDescription: form.startupDescription,
        startupFoundingDate: form.startupFoundingDate || null,
      }),
      ...(form.role === 'Mentor' && {
        mentorExpertise: form.mentorExpertise,
        mentorExperienceYears: form.mentorExperienceYears
          ? Number(form.mentorExperienceYears)
          : null,
        mentorOrganization: form.mentorOrganization,
      }),
    };

    try {
      await register(payload);
      navigate('/');
    } catch (err) {
      setError(err.response?.data ?? 'Registration failed.');
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="container d-flex justify-content-center align-items-center" style={{ minHeight: '80vh', padding: '2rem 0' }}>
      <div className="card w-100 p-4 p-md-5" style={{ maxWidth: '500px' }}>
        <h2 className="mb-4 text-center">Register</h2>
      <form onSubmit={handleSubmit}>
        <div className="mb-3">
          <label className="form-label">Name</label>
          <input className="form-control" value={form.name} onChange={(e) => update('name', e.target.value)} required />
        </div>
        <div className="mb-3">
          <label className="form-label">Email</label>
          <input type="email" className="form-control" value={form.email} onChange={(e) => update('email', e.target.value)} required />
        </div>
        <div className="mb-3">
          <label className="form-label">Password</label>
          <input type="password" className="form-control" value={form.password} onChange={(e) => update('password', e.target.value)} required minLength={8} />
        </div>
        <div className="mb-3">
          <label className="form-label">Role</label>
          <select className="form-select" value={form.role} onChange={(e) => update('role', e.target.value)}>
            <option value="Founder">Founder</option>
            <option value="Mentor">Mentor</option>
            <option value="Admin">Admin</option>
          </select>
        </div>

        {form.role === 'Founder' && (
          <fieldset className="border rounded p-3 mb-3">
            <legend className="fs-6 px-2 w-auto">Your startup</legend>
            <div className="mb-2">
              <label className="form-label">Startup name</label>
              <input className="form-control" value={form.startupName} onChange={(e) => update('startupName', e.target.value)} required />
            </div>
            <div className="mb-2">
              <label className="form-label">Domain</label>
              <input className="form-control" value={form.startupDomain} onChange={(e) => update('startupDomain', e.target.value)} />
            </div>
            <div className="mb-2">
              <label className="form-label">Description</label>
              <textarea className="form-control" value={form.startupDescription} onChange={(e) => update('startupDescription', e.target.value)} />
            </div>
            <div className="mb-2">
              <label className="form-label">Founding date</label>
              <input type="date" className="form-control" value={form.startupFoundingDate} onChange={(e) => update('startupFoundingDate', e.target.value)} />
            </div>
          </fieldset>
        )}

        {form.role === 'Mentor' && (
          <fieldset className="border rounded p-3 mb-3">
            <legend className="fs-6 px-2 w-auto">Your mentor profile</legend>
            <div className="mb-2">
              <label className="form-label">Expertise</label>
              <input className="form-control" value={form.mentorExpertise} onChange={(e) => update('mentorExpertise', e.target.value)} required />
            </div>
            <div className="mb-2">
              <label className="form-label">Years of experience</label>
              <input type="number" min="0" className="form-control" value={form.mentorExperienceYears} onChange={(e) => update('mentorExperienceYears', e.target.value)} />
            </div>
            <div className="mb-2">
              <label className="form-label">Organization</label>
              <input className="form-control" value={form.mentorOrganization} onChange={(e) => update('mentorOrganization', e.target.value)} />
            </div>
          </fieldset>
        )}

        {error && <div className="alert alert-danger py-2">{JSON.stringify(error)}</div>}
        <button type="submit" className="btn btn-primary w-100" disabled={loading}>
          {loading ? 'Registering...' : 'Register'}
        </button>
      </form>
      <p className="mt-4 text-center mb-0">
        Already have an account? <Link to="/login" className="text-decoration-none">Log in here</Link>
      </p>
      </div>
    </div>
  );
}
