import { useState, useEffect, useCallback } from 'react';
import './App.css';

const API_BASE = 'http://localhost:5033/api';

function getUserIdFromToken(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return Number(
      payload.nameid ||
      payload.sub ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
    );
  } catch {
    return null;
  }
}

function getUserNameFromToken(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return (
      payload.unique_name ||
      payload.name ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ||
      'User'
    );
  } catch {
    return 'User';
  }
}

function formatCurrency(amount) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 2,
  }).format(amount || 0);
}

const Icons = {
  Wallet: () => (
    <svg className="brand-icon" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
    </svg>
  ),
  Dashboard: () => (
    <svg width="18" height="18" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <rect x="3" y="3" width="7" height="9" rx="1" />
      <rect x="14" y="3" width="7" height="5" rx="1" />
      <rect x="14" y="12" width="7" height="9" rx="1" />
      <rect x="3" y="16" width="7" height="5" rx="1" />
    </svg>
  ),
  Trips: () => (
    <svg width="18" height="18" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
    </svg>
  ),
  Plus: () => (
    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4v16m8-8H4" />
    </svg>
  ),
  LogOut: () => (
    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
    </svg>
  ),
  Check: () => (
    <svg width="14" height="14" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
    </svg>
  ),
  Clock: () => (
    <svg width="14" height="14" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <circle cx="12" cy="12" r="10" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 6v6l4 2" />
    </svg>
  ),
  Refresh: () => (
    <svg width="15" height="15" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
    </svg>
  ),
  ArrowLeft: () => (
    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
    </svg>
  ),
  Users: () => (
    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
    </svg>
  ),
  Receipt: () => (
    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2z" />
    </svg>
  ),
  Compass: () => (
    <svg width="22" height="22" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <circle cx="12" cy="12" r="10" />
      <polygon points="16.24 7.76 14.12 14.12 7.76 16.24 9.88 9.88 16.24 7.76" fill="currentColor" fillOpacity="0.25" />
    </svg>
  ),
  Plane: () => (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="currentColor">
      <path d="M21 16v-2l-8-5V3.5c0-.83-.67-1.5-1.5-1.5S10 2.67 10 3.5V9l-8 5v2l8-2.5V19l-2 1.5V22l3.5-1 3.5 1v-1.5L13 19v-5.5l8 2.5z"/>
    </svg>
  ),
  MapPin: () => (
    <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
    </svg>
  ),
  ShieldCheck: () => (
    <svg width="16" height="16" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
    </svg>
  ),
  Suitcase: () => (
    <svg width="20" height="20" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
      <path d="M16 21V5a2 2 0 00-2-2h-4a2 2 0 00-2 2v16" />
    </svg>
  ),
  Eye: () => (
    <svg width="18" height="18" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
      <path strokeLinecap="round" strokeLinejoin="round" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
    </svg>
  ),
  EyeOff: () => (
    <svg width="18" height="18" fill="none" stroke="currentColor" viewBox="0 0 24 24" strokeWidth="2">
      <path strokeLinecap="round" strokeLinejoin="round" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
    </svg>
  ),
};

function TripSplitLogo({ size = 36 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 36 36" fill="none" xmlns="http://www.w3.org/2000/svg" style={{ flexShrink: 0 }}>
      <defs>
        <linearGradient id="tsGrad" x1="0" y1="0" x2="36" y2="36" gradientUnits="userSpaceOnUse">
          <stop stopColor="#2563EB" />
          <stop offset="1" stopColor="#1D4ED8" />
        </linearGradient>
      </defs>
      <rect width={36} height={36} rx="10" fill="url(#tsGrad)" />
      <path d="M18 7L27 21L18 17.5L9 21L18 7Z" fill="#FFFFFF" />
      <path d="M18 17.5V28" stroke="#93C5FD" strokeWidth="2.5" strokeLinecap="round" />
      <circle cx="18" cy="17.5" r="2" fill="#1D4ED8" />
    </svg>
  );
}

export default function App() {
  const [token, setToken] = useState(localStorage.getItem('token') || '');
  const [activeTab, setActiveTab] = useState('dashboard');

  const [authMode, setAuthMode] = useState('login');
  const [authName, setAuthName] = useState('');
  const [authEmail, setAuthEmail] = useState('');
  const [authPassword, setAuthPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [authError, setAuthError] = useState('');
  const [authSuccess, setAuthSuccess] = useState('');
  const [authLoading, setAuthLoading] = useState(false);

  const [dashboard, setDashboard] = useState(null);
  const [dashboardLoading, setDashboardLoading] = useState(false);

  const [trips, setTrips] = useState([]);
  const [tripsLoading, setTripsLoading] = useState(false);
  const [allUsers, setAllUsers] = useState([]);

  const [newTripTitle, setNewTripTitle] = useState('');
  const [newTripDesc, setNewTripDesc] = useState('');
  const [newTripMembers, setNewTripMembers] = useState([]);
  const [showCreateTrip, setShowCreateTrip] = useState(false);
  const [tripError, setTripError] = useState('');

  const [selectedTrip, setSelectedTrip] = useState(null);
  const [selectedTripLoading, setSelectedTripLoading] = useState(false);
  const [addMemberUserId, setAddMemberUserId] = useState('');
  const [memberActionError, setMemberActionError] = useState('');

  const [newExpDesc, setNewExpDesc] = useState('');
  const [newExpAmount, setNewExpAmount] = useState('');
  const [newExpPaidBy, setNewExpPaidBy] = useState('');
  const [newExpParticipants, setNewExpParticipants] = useState([]);
  const [expenseActionError, setExpenseActionError] = useState('');
  const [expenseActionSuccess, setExpenseActionSuccess] = useState('');
  const [settlingSplitId, setSettlingSplitId] = useState(null);
  const [settleToast, setSettleToast] = useState('');
  const [dashboardSubTab, setDashboardSubTab] = useState('receivables'); // 'receivables' | 'payables' | 'history'

  const currentUserId = token ? getUserIdFromToken(token) : null;
  const currentUserName = token ? getUserNameFromToken(token) : 'User';

  const authHeaders = useCallback(() => {
    return {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    };
  }, [token]);

  const handleLogin = async (e) => {
    e.preventDefault();
    setAuthError('');
    setAuthSuccess('');
    setAuthLoading(true);

    try {
      const res = await fetch(`${API_BASE}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: authEmail, password: authPassword }),
      });

      if (!res.ok) {
        const errorText = await res.text();
        let msg = errorText || 'Invalid credentials';
        try {
          const parsed = JSON.parse(errorText);
          msg = parsed.message || parsed.title || msg;
        } catch {
          if (errorText.length > 200 || errorText.includes('SqlException') || errorText.includes('at Microsoft.')) {
            msg = 'Database connection error. Please ensure the backend server is restarted.';
          }
        }
        throw new Error(msg);
      }

      const data = await res.json();
      localStorage.setItem('token', data.token);
      setToken(data.token);
      setAuthPassword('');
    } catch (err) {
      setAuthError(err.message);
    } finally {
      setAuthLoading(false);
    }
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    setAuthError('');
    setAuthSuccess('');
    setAuthLoading(true);

    try {
      const res = await fetch(`${API_BASE}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          name: authName,
          email: authEmail,
          password: authPassword,
        }),
      });

      if (!res.ok) {
        const errorText = await res.text();
        let msg = errorText || 'Registration failed';
        try {
          const parsed = JSON.parse(errorText);
          msg = parsed.message || parsed.title || msg;
        } catch {
          if (errorText.length > 200 || errorText.includes('SqlException') || errorText.includes('at Microsoft.')) {
            msg = 'Database connection error. Please ensure the backend server is restarted.';
          }
        }
        throw new Error(msg);
      }

      setAuthMode('login');
      setAuthPassword('');
      setAuthSuccess('Account registered successfully. You can now sign in.');
    } catch (err) {
      setAuthError(err.message);
    } finally {
      setAuthLoading(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    setToken('');
    setDashboard(null);
    setTrips([]);
    setSelectedTrip(null);
    setActiveTab('dashboard');
  };

  const loadDashboard = useCallback(async () => {
    if (!currentUserId || !token) return;
    setDashboardLoading(true);
    try {
      const res = await fetch(`${API_BASE}/users/${currentUserId}/dashboard`, {
        headers: authHeaders(),
      });
      if (res.ok) {
        const data = await res.json();
        setDashboard(data);
      }
    } catch (err) {
      console.error('Failed to load dashboard:', err);
    } finally {
      setDashboardLoading(false);
    }
  }, [currentUserId, token, authHeaders]);

  const loadTrips = useCallback(async () => {
    if (!token) return;
    setTripsLoading(true);
    try {
      const res = await fetch(`${API_BASE}/trips`, {
        headers: authHeaders(),
      });
      if (res.ok) {
        const data = await res.json();
        setTrips(data);
      }
    } catch (err) {
      console.error('Failed to load trips:', err);
    } finally {
      setTripsLoading(false);
    }
  }, [token, authHeaders]);

  const loadAllUsers = useCallback(async () => {
    if (!token) return;
    try {
      const res = await fetch(`${API_BASE}/users`, {
        headers: authHeaders(),
      });
      if (res.ok) {
        const data = await res.json();
        setAllUsers(data);
      }
    } catch (err) {
      console.error('Failed to load users:', err);
    }
  }, [token, authHeaders]);

  const openTrip = async (tripId) => {
    setSelectedTripLoading(true);
    setMemberActionError('');
    setExpenseActionError('');
    setExpenseActionSuccess('');

    try {
      const res = await fetch(`${API_BASE}/trips/${tripId}`, {
        headers: authHeaders(),
      });
      if (!res.ok) throw new Error('Failed to load trip details');
      const data = await res.json();

      // Ensure each expense has splits populated
      const enrichedExpenses = await Promise.all(
        (data.expenses || []).map(async (exp) => {
          if (exp.splits && exp.splits.length > 0) return exp;
          try {
            const expRes = await fetch(`${API_BASE}/expenses/${exp.id}`, {
              headers: authHeaders(),
            });
            if (expRes.ok) {
              const fullExp = await expRes.json();
              return { ...exp, splits: fullExp.splits || [] };
            }
          } catch {
            // fallback
          }
          return exp;
        })
      );

      setSelectedTrip({ ...data, expenses: enrichedExpenses });

      const memberIds = (data.members || []).map((m) => m.userId);
      setNewExpParticipants(memberIds);
      setNewExpPaidBy(currentUserId ? String(currentUserId) : (memberIds[0] ? String(memberIds[0]) : ''));
    } catch (err) {
      setTripError(err.message);
    } finally {
      setSelectedTripLoading(false);
    }
  };

  const handleCreateTrip = async (e) => {
    e.preventDefault();
    setTripError('');

    try {
      const res = await fetch(`${API_BASE}/trips`, {
        method: 'POST',
        headers: authHeaders(),
        body: JSON.stringify({
          title: newTripTitle,
          description: newTripDesc,
          createdByUserId: currentUserId,
          initialMemberUserIds: newTripMembers,
        }),
      });

      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || 'Failed to create trip');
      }

      setNewTripTitle('');
      setNewTripDesc('');
      setNewTripMembers([]);
      setShowCreateTrip(false);
      await loadTrips();
    } catch (err) {
      setTripError(err.message);
    }
  };

  const handleAddMember = async (e) => {
    e.preventDefault();
    if (!selectedTrip || !addMemberUserId) return;
    setMemberActionError('');

    try {
      const res = await fetch(`${API_BASE}/trips/${selectedTrip.id}/members`, {
        method: 'POST',
        headers: authHeaders(),
        body: JSON.stringify({ userId: Number(addMemberUserId) }),
      });

      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || 'Failed to add member');
      }

      setAddMemberUserId('');
      await openTrip(selectedTrip.id);
      await loadTrips();
    } catch (err) {
      setMemberActionError(err.message);
    }
  };

  const handleAddExpense = async (e) => {
    e.preventDefault();
    if (!selectedTrip) return;
    setExpenseActionError('');
    setExpenseActionSuccess('');

    if (newExpParticipants.length === 0) {
      setExpenseActionError('Please select at least one participant.');
      return;
    }

    try {
      const res = await fetch(`${API_BASE}/expenses`, {
        method: 'POST',
        headers: authHeaders(),
        body: JSON.stringify({
          description: newExpDesc,
          amount: parseFloat(newExpAmount),
          tripId: selectedTrip.id,
          paidByUserId: Number(newExpPaidBy),
          participantUserIds: newExpParticipants,
        }),
      });

      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || 'Failed to add expense');
      }

      setNewExpDesc('');
      setNewExpAmount('');
      setExpenseActionSuccess('Expense split and recorded successfully.');
      await openTrip(selectedTrip.id);
      await loadDashboard();
    } catch (err) {
      setExpenseActionError(err.message);
    }
  };

  const handleSettleSplit = async (splitId) => {
    setSettlingSplitId(splitId);
    try {
      const res = await fetch(`${API_BASE}/expenses/splits/${splitId}/settle`, {
        method: 'PUT',
        headers: authHeaders(),
      });

      if (!res.ok) {
        throw new Error('Failed to settle split');
      }

      setSettleToast('Payment marked as settled successfully.');
      setTimeout(() => setSettleToast(''), 4000);

      if (selectedTrip) {
        await openTrip(selectedTrip.id);
      }
      await loadDashboard();
    } catch (err) {
      console.error(err);
    } finally {
      setSettlingSplitId(null);
    }
  };

  const toggleTripMemberSelection = (userId) => {
    setNewTripMembers((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  const toggleExpenseParticipant = (userId) => {
    setNewExpParticipants((prev) =>
      prev.includes(userId) ? prev.filter((id) => id !== userId) : [...prev, userId]
    );
  };

  useEffect(() => {
    if (token) {
      loadDashboard();
      loadTrips();
      loadAllUsers();
    }
  }, [token, loadDashboard, loadTrips, loadAllUsers]);

  if (!token) {
    return (
      <div className="landing-page">
        {/* Navigation Bar */}
        <header className="landing-navbar">
          <div className="landing-nav-inner">
            <div className="landing-brand">
              <TripSplitLogo size={36} />
              <div className="brand-text-block">
                <span className="brand-title">Trip<span className="brand-title-accent">Split</span></span>
                <span className="brand-subtitle">Group Travel Expenses</span>
              </div>
            </div>

            <div className="landing-nav-actions">
              <button
                type="button"
                className={`btn btn-sm ${authMode === 'login' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => {
                  setAuthMode('login');
                  setAuthError('');
                  setAuthSuccess('');
                }}
              >
                Sign In
              </button>
              <button
                type="button"
                className={`btn btn-sm ${authMode === 'register' ? 'btn-primary' : 'btn-secondary'}`}
                onClick={() => {
                  setAuthMode('register');
                  setAuthError('');
                  setAuthSuccess('');
                }}
              >
                Create Account
              </button>
            </div>
          </div>
        </header>

        {/* Hero Section */}
        <main className="landing-hero">
          <div className="landing-hero-container">
            {/* Left Column: Minimal Crisp Value Proposition */}
            <div className="hero-content">
              <div className="hero-pill">
                <Icons.Plane />
                <span>Group Travel</span>
              </div>

              <h1 className="hero-title">
                Split travel expenses.<br />
                <span className="hero-title-highlight">Fair and simple.</span>
              </h1>

              <p className="hero-description">
                Track shared costs and settle balances with companions effortlessly.
              </p>

              <div className="hero-bullets">
                <div className="bullet-item">
                  <span className="bullet-icon"><Icons.Check /></span>
                  <span>Equal splits across travelers</span>
                </div>
                <div className="bullet-item">
                  <span className="bullet-icon"><Icons.Check /></span>
                  <span>Real-time balance tracking</span>
                </div>
                <div className="bullet-item">
                  <span className="bullet-icon"><Icons.Check /></span>
                  <span>Instant one-click settlements</span>
                </div>
              </div>
            </div>

            {/* Right Column: Clean Auth Card */}
            <div className="hero-auth-pane">
              <div className="auth-card">
                <div className="auth-header">
                  <div className="auth-logo">
                    <TripSplitLogo size={42} />
                  </div>
                  <h2 className="auth-title">
                    {authMode === 'login' ? 'Sign In to TripSplit' : 'Create Account'}
                  </h2>
                  <p className="auth-subtitle">
                    {authMode === 'login'
                      ? 'Access your trips and balances'
                      : 'Start tracking shared group expenses'}
                  </p>
                </div>

                <div className="auth-tabs">
                  <button
                    type="button"
                    className={`auth-tab ${authMode === 'login' ? 'active' : ''}`}
                    onClick={() => {
                      setAuthMode('login');
                      setAuthError('');
                      setAuthSuccess('');
                    }}
                  >
                    Sign In
                  </button>
                  <button
                    type="button"
                    className={`auth-tab ${authMode === 'register' ? 'active' : ''}`}
                    onClick={() => {
                      setAuthMode('register');
                      setAuthError('');
                      setAuthSuccess('');
                    }}
                  >
                    Create Account
                  </button>
                </div>

                {authError && (
                  <div className="alert alert-error" style={{ marginBottom: 16 }}>
                    {authError}
                  </div>
                )}

                {authSuccess && (
                  <div className="alert alert-success" style={{ marginBottom: 16 }}>
                    {authSuccess}
                  </div>
                )}

                <form onSubmit={authMode === 'login' ? handleLogin : handleRegister} className="form-stack">
                  {authMode === 'register' && (
                    <div className="form-group">
                      <label className="form-label">Full Name</label>
                      <input
                        type="text"
                        className="form-input"
                        placeholder="Enter your full name"
                        value={authName}
                        onChange={(e) => setAuthName(e.target.value)}
                        required
                      />
                    </div>
                  )}

                  <div className="form-group">
                    <label className="form-label">Email Address</label>
                    <input
                      type="email"
                      className="form-input"
                      placeholder="name@example.com"
                      value={authEmail}
                      onChange={(e) => setAuthEmail(e.target.value)}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Password</label>
                    <div className="input-password-wrapper">
                      <input
                        type={showPassword ? 'text' : 'password'}
                        className="form-input"
                        placeholder="Enter your password"
                        value={authPassword}
                        onChange={(e) => setAuthPassword(e.target.value)}
                        required
                        minLength={6}
                      />
                      <button
                        type="button"
                        className="input-password-toggle"
                        onClick={() => setShowPassword(!showPassword)}
                        title={showPassword ? 'Hide password' : 'Show password'}
                        aria-label={showPassword ? 'Hide password' : 'Show password'}
                        tabIndex={-1}
                      >
                        {showPassword ? <Icons.EyeOff /> : <Icons.Eye />}
                      </button>
                    </div>
                  </div>

                  <button type="submit" className="btn btn-primary btn-block btn-lg" disabled={authLoading}>
                    {authLoading
                      ? 'Processing...'
                      : authMode === 'login'
                      ? 'Sign In'
                      : 'Create Account'}
                  </button>
                </form>
              </div>
            </div>
          </div>
        </main>

        {/* Minimal Feature Row */}
        <section className="landing-features-minimal">
          <div className="minimal-features-grid">
            <div className="min-feature-item">
              <div className="min-feature-icon">
                <Icons.MapPin />
              </div>
              <div className="min-feature-content">
                <strong>Group Trips</strong>
                <p>Create itineraries and invite companions</p>
              </div>
            </div>

            <div className="min-feature-item">
              <div className="min-feature-icon">
                <Icons.Receipt />
              </div>
              <div className="min-feature-content">
                <strong>Automatic Splitting</strong>
                <p>Equal cost division per traveler</p>
              </div>
            </div>

            <div className="min-feature-item">
              <div className="min-feature-icon">
                <Icons.Dashboard />
              </div>
              <div className="min-feature-content">
                <strong>Clear Balances</strong>
                <p>Transparent who-owes-who ledgers</p>
              </div>
            </div>
          </div>
        </section>

        {/* Clean Footer */}
        <footer className="landing-footer-minimal">
          <p>© 2026 TripSplit • Group Travel Expense Splitter</p>
        </footer>
      </div>
    );
  }

  return (
    <div className="app-shell">
      <header className="navbar">
        <div className="nav-container">
          <div className="nav-brand">
            <TripSplitLogo size={34} />
            <div className="brand-text-block">
              <span className="brand-title">Trip<span className="brand-title-accent">Split</span></span>
              <span className="brand-subtitle">Group Travel Ledger</span>
            </div>
          </div>

          <nav className="nav-links">
            <button
              className={`nav-link ${activeTab === 'dashboard' && !selectedTrip ? 'active' : ''}`}
              onClick={() => {
                setActiveTab('dashboard');
                setSelectedTrip(null);
                loadDashboard();
              }}
            >
              <Icons.Dashboard />
              <span>Dashboard</span>
            </button>
            <button
              className={`nav-link ${activeTab === 'trips' || selectedTrip ? 'active' : ''}`}
              onClick={() => {
                setActiveTab('trips');
                setSelectedTrip(null);
                loadTrips();
              }}
            >
              <Icons.Trips />
              <span>Trips</span>
            </button>
          </nav>

          <div className="nav-user">
            <div className="user-pill">
              <span className="user-avatar">{currentUserName.charAt(0).toUpperCase()}</span>
              <span>{currentUserName}</span>
            </div>
            <button className="btn btn-secondary btn-sm" onClick={handleLogout} title="Sign Out">
              <Icons.LogOut />
              <span>Sign Out</span>
            </button>
          </div>
        </div>
      </header>

      <main className="main-content">
        {selectedTrip ? (
          <div>
            {/* Top Navigation & Breadcrumb */}
            <div className="trip-back-bar">
              <button
                type="button"
                className="back-btn"
                onClick={() => {
                  setSelectedTrip(null);
                  loadTrips();
                }}
              >
                <Icons.ArrowLeft />
                <span>Back to Trips</span>
              </button>
              <div className="trip-breadcrumb">
                <span>Trips</span>
                <span className="breadcrumb-separator">/</span>
                <span className="breadcrumb-current">{selectedTrip.title}</span>
              </div>
            </div>

            {/* Trip Hero Banner */}
            <div className="trip-hero-banner">
              <div className="trip-banner-main">
                <div className="trip-banner-tag">
                  <Icons.Plane />
                  <span>Trip Itinerary</span>
                </div>
                <h1 className="trip-banner-title">{selectedTrip.title}</h1>
                {selectedTrip.description && (
                  <p className="trip-banner-desc">{selectedTrip.description}</p>
                )}
              </div>

              <div className="trip-banner-stats">
                <div className="banner-stat-box">
                  <span className="stat-label">Total Expenses</span>
                  <strong className="stat-value">
                    {formatCurrency(
                      (selectedTrip.expenses || []).reduce(
                        (acc, c) => acc + (parseFloat(c.amount) || 0),
                        0
                      )
                    )}
                  </strong>
                </div>
                <div className="banner-stat-box">
                  <span className="stat-label">Travelers</span>
                  <strong className="stat-value">
                    {(selectedTrip.members || []).length} {(selectedTrip.members || []).length === 1 ? 'Member' : 'Members'}
                  </strong>
                </div>
                <div className="banner-stat-box">
                  <span className="stat-label">Lead Organizer</span>
                  <strong className="stat-value">
                    {selectedTrip.createdByUserName || 'Host'}
                  </strong>
                </div>
              </div>
            </div>

            {/* Layout: Left = Expenses, Right = Add Expense & Members */}
            <div className="trip-details-layout">
              <div>
                <div className="card">
                  <div className="card-header">
                    <h2 className="card-title">
                      <Icons.Receipt />
                      <span>Recorded Expenses</span>
                    </h2>
                    <span className="badge badge-neutral">
                      {(selectedTrip.expenses || []).length}{' '}
                      {(selectedTrip.expenses || []).length === 1 ? 'Entry' : 'Entries'}
                    </span>
                  </div>

                  {selectedTripLoading ? (
                    <p style={{ color: 'var(--text-muted)' }}>Loading expenses...</p>
                  ) : (selectedTrip.expenses || []).length === 0 ? (
                    <div className="empty-state">
                      <div className="empty-state-icon-circle">
                        <Icons.Receipt />
                      </div>
                      <p className="empty-state-title">No expenses recorded yet</p>
                      <p className="empty-state-desc">
                        Use the "Add Expense" form to log group flights, accommodations, fuel, or meals.
                      </p>
                    </div>
                  ) : (
                    <div className="expenses-stream">
                      {selectedTrip.expenses.map((exp) => (
                        <div key={exp.id} className="expense-card-item">
                          <div className="expense-item-top">
                            <div>
                              <div className="expense-title">{exp.description}</div>
                              <div className="expense-meta">
                                Paid by <strong>{exp.paidByUserName}</strong> •{' '}
                                {new Date(exp.createdAt).toLocaleDateString(undefined, {
                                  month: 'short',
                                  day: 'numeric',
                                  year: 'numeric',
                                })}
                              </div>
                            </div>
                            <div className="expense-amount">{formatCurrency(exp.amount)}</div>
                          </div>

                          {exp.splits && exp.splits.length > 0 && (
                            <div className="splits-list">
                              <div className="splits-header">
                                Split Breakdown ({exp.splits.length} shares)
                              </div>
                              {exp.splits.map((s) => (
                                <div key={s.id} className="split-item">
                                  <div className="split-user">
                                    <span className="split-avatar">
                                      {s.userName ? s.userName.charAt(0).toUpperCase() : 'U'}
                                    </span>
                                    <div className="split-user-text">
                                      <span className="split-user-name">
                                        {s.userName} {s.userId === currentUserId ? '(You)' : ''}
                                      </span>
                                      <span className="split-user-owes">owes</span>
                                      <strong className="split-user-amount">
                                        {formatCurrency(s.amountOwed)}
                                      </strong>
                                    </div>
                                  </div>
                                  <div className="split-actions">
                                    <span className={`badge ${s.isPaid ? 'badge-success' : 'badge-warning'}`}>
                                      {s.isPaid ? <Icons.Check /> : <Icons.Clock />}
                                      <span>{s.status}</span>
                                    </span>
                                    {!s.isPaid && (
                                      <button
                                        type="button"
                                        className="btn btn-success btn-sm"
                                        disabled={settlingSplitId === s.id}
                                        onClick={() => handleSettleSplit(s.id)}
                                      >
                                        {settlingSplitId === s.id ? 'Settling...' : 'Mark Settled'}
                                      </button>
                                    )}
                                  </div>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              <div>
                {/* Add Expense Form Card */}
                <div className="card" style={{ marginBottom: 24 }}>
                  <div className="card-header">
                    <h2 className="card-title">
                      <Icons.Plus />
                      <span>Add Expense</span>
                    </h2>
                  </div>

                  {expenseActionError && (
                    <div className="alert alert-error" style={{ marginBottom: 14 }}>
                      {expenseActionError}
                    </div>
                  )}

                  {expenseActionSuccess && (
                    <div className="alert alert-success" style={{ marginBottom: 14 }}>
                      {expenseActionSuccess}
                    </div>
                  )}

                  <form onSubmit={handleAddExpense} className="form-stack">
                    <div className="form-group">
                      <label className="form-label">Expense Description</label>
                      <input
                        type="text"
                        className="form-input"
                        placeholder="e.g. Hotel stay, Fuel, Dinner"
                        value={newExpDesc}
                        onChange={(e) => setNewExpDesc(e.target.value)}
                        required
                      />
                    </div>

                    <div className="form-group">
                      <label className="form-label">Total Amount (INR)</label>
                      <input
                        type="number"
                        step="0.01"
                        min="0.01"
                        className="form-input"
                        placeholder="0.00"
                        value={newExpAmount}
                        onChange={(e) => setNewExpAmount(e.target.value)}
                        required
                      />
                    </div>

                    <div className="form-group">
                      <label className="form-label">Paid By</label>
                      <select
                        className="form-input"
                        value={newExpPaidBy}
                        onChange={(e) => setNewExpPaidBy(e.target.value)}
                      >
                        {(selectedTrip.members || []).map((m) => (
                          <option key={m.userId} value={m.userId}>
                            {m.userName} {m.userId === currentUserId ? '(You)' : ''}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="form-group">
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                        <label className="form-label" style={{ marginBottom: 0 }}>
                          Split Between ({newExpParticipants.length})
                        </label>
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          style={{ padding: '2px 8px', fontSize: '0.75rem' }}
                          onClick={() => {
                            const allMemberIds = (selectedTrip.members || []).map((m) => m.userId);
                            setNewExpParticipants(
                              newExpParticipants.length === allMemberIds.length ? [] : allMemberIds
                            );
                          }}
                        >
                          {newExpParticipants.length === (selectedTrip.members || []).length
                            ? 'Deselect All'
                            : 'Select All'}
                        </button>
                      </div>

                      {/* Interactive Participant Chips */}
                      <div className="participant-chips">
                        {(selectedTrip.members || []).map((m) => {
                          const isSelected = newExpParticipants.includes(m.userId);
                          return (
                            <button
                              key={m.userId}
                              type="button"
                              className={`participant-chip ${isSelected ? 'active' : ''}`}
                              onClick={() => toggleExpenseParticipant(m.userId)}
                            >
                              <span className="chip-avatar">{m.userName.charAt(0).toUpperCase()}</span>
                              <span>{m.userName} {m.userId === currentUserId ? '(You)' : ''}</span>
                              {isSelected && <Icons.Check />}
                            </button>
                          );
                        })}
                      </div>
                    </div>

                    {newExpAmount && newExpParticipants.length > 0 && (
                      <div className="split-preview">
                        <span>Equal share:</span>
                        <strong>
                          {formatCurrency(parseFloat(newExpAmount) / newExpParticipants.length)} / person
                        </strong>
                      </div>
                    )}

                    <button type="submit" className="btn btn-primary btn-block btn-lg">
                      Record Expense & Split
                    </button>
                  </form>
                </div>

                {/* Trip Members Card */}
                <div className="card">
                  <div className="card-header">
                    <h2 className="card-title">
                      <Icons.Users />
                      <span>Trip Members</span>
                    </h2>
                    <span className="badge badge-neutral">
                      {(selectedTrip.members || []).length} Travelers
                    </span>
                  </div>

                  <div className="members-stack">
                    {(selectedTrip.members || []).map((m) => (
                      <div key={m.id} className="member-row">
                        <div className="member-row-info">
                          <span className="user-avatar">
                            {m.userName.charAt(0).toUpperCase()}
                          </span>
                          <div>
                            <div className="member-name">
                              {m.userName} {m.userId === currentUserId && <span className="member-you-tag">(You)</span>}
                            </div>
                            <div className="member-email">
                              {m.email}
                            </div>
                          </div>
                        </div>
                        {selectedTrip.createdByUserId === m.userId && (
                          <span className="badge badge-primary">Host</span>
                        )}
                      </div>
                    ))}
                  </div>

                  {memberActionError && (
                    <div className="alert alert-error" style={{ marginBottom: 12 }}>
                      {memberActionError}
                    </div>
                  )}

                  {(() => {
                    const availableUsers = allUsers.filter(
                      (u) => !(selectedTrip.members || []).some((m) => m.userId === u.id)
                    );

                    if (availableUsers.length === 0) {
                      return (
                        <div className="all-members-added-note">
                          <Icons.Check />
                          <span>All registered companions are currently in this trip</span>
                        </div>
                      );
                    }

                    return (
                      <form
                        onSubmit={handleAddMember}
                        className="form-stack"
                        style={{ borderTop: '1px solid var(--border-subtle)', paddingTop: 14 }}
                      >
                        <div className="form-group">
                          <label className="form-label">Add Companion by Email</label>
                          <select
                            className="form-input"
                            value={addMemberUserId}
                            onChange={(e) => setAddMemberUserId(e.target.value)}
                            required
                          >
                            <option value="">Select registered user...</option>
                            {availableUsers.map((u) => (
                              <option key={u.id} value={u.id}>
                                {u.name} ({u.email})
                              </option>
                            ))}
                          </select>
                        </div>

                        <button
                          type="submit"
                          className="btn btn-secondary btn-block"
                          disabled={!addMemberUserId}
                        >
                          <Icons.Plus />
                          <span>Add to Trip</span>
                        </button>
                      </form>
                    );
                  })()}
                </div>
              </div>
            </div>
          </div>
        ) : activeTab === 'dashboard' ? (
          <div>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 24 }}>
              <div>
                <h1 style={{ fontSize: '1.5rem', fontWeight: 700, letterSpacing: '-0.02em', color: 'var(--text-main)' }}>
                  Financial Overview
                </h1>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.9rem' }}>
                  Track what you owe, what you are owed, and settle accounts
                </p>
              </div>
              <button
                className="btn btn-secondary btn-sm"
                onClick={loadDashboard}
                disabled={dashboardLoading}
              >
                <Icons.Refresh />
                <span>{dashboardLoading ? 'Refreshing...' : 'Refresh'}</span>
              </button>
            </div>

            {settleToast && (
              <div className="alert alert-success" style={{ marginBottom: 18 }}>
                <Icons.Check />
                <span>{settleToast}</span>
              </div>
            )}

            <div className="kpi-grid">
              <div className="kpi-card">
                <span className="kpi-label">Net Balance</span>
                <div
                  className={`kpi-value ${
                    (dashboard?.netBalance || 0) > 0
                      ? 'positive'
                      : (dashboard?.netBalance || 0) < 0
                      ? 'negative'
                      : 'neutral'
                  }`}
                >
                  {formatCurrency(dashboard?.netBalance || 0)}
                </div>
                <span className="kpi-subtext">
                  {(dashboard?.netBalance || 0) > 0
                    ? 'You are owed overall across all trips'
                    : (dashboard?.netBalance || 0) < 0
                    ? 'You have pending amounts to settle'
                    : 'All your expense balances are settled'}
                </span>
              </div>

              <div className="kpi-card">
                <span className="kpi-label">You Owe</span>
                <div className="kpi-value negative">
                  {formatCurrency(dashboard?.totalOwed || 0)}
                </div>
                <span className="kpi-subtext">
                  {(dashboard?.whatYouOwe || []).filter((w) => !w.isPaid).length} pending payments
                </span>
              </div>

              <div className="kpi-card">
                <span className="kpi-label">You Are Owed</span>
                <div className="kpi-value positive">
                  {formatCurrency(dashboard?.totalReceivable || 0)}
                </div>
                <span className="kpi-subtext">
                  {(dashboard?.whatOthersOweYou || []).filter((w) => !w.isPaid).length} pending collections
                </span>
              </div>
            </div>

            {/* Unified Financial Ledger Card */}
            <div className="card ledger-card">
              <div className="ledger-card-header">
                <div className="ledger-title-block">
                  <div className="ledger-pill-tag">
                    <Icons.Receipt />
                    <span>Financial Ledger</span>
                  </div>
                  <h2 className="ledger-title">Group Activity & Balances</h2>
                  <p className="ledger-subtitle">
                    Track companion dues, pending reimbursements, and settlement history
                  </p>
                </div>

                <div className="ledger-tab-bar">
                  <button
                    type="button"
                    className={`ledger-tab-btn ${dashboardSubTab === 'receivables' ? 'active' : ''}`}
                    onClick={() => setDashboardSubTab('receivables')}
                  >
                    <span>Owed to You</span>
                    <span className="tab-pill">
                      {(dashboard?.whatOthersOweYou || []).length}
                    </span>
                  </button>

                  <button
                    type="button"
                    className={`ledger-tab-btn ${dashboardSubTab === 'payables' ? 'active' : ''}`}
                    onClick={() => setDashboardSubTab('payables')}
                  >
                    <span>Expenses You Owe</span>
                    <span className="tab-pill">
                      {(dashboard?.whatYouOwe || []).length}
                    </span>
                  </button>

                  <button
                    type="button"
                    className={`ledger-tab-btn ${dashboardSubTab === 'history' ? 'active' : ''}`}
                    onClick={() => setDashboardSubTab('history')}
                  >
                    <span>Settlement History</span>
                    <span className="tab-pill">
                      {(dashboard?.allBalances || []).length}
                    </span>
                  </button>
                </div>
              </div>

              {/* Tab 1: Expenses Owed to You */}
              {dashboardSubTab === 'receivables' && (
                <div>
                  {(dashboard?.whatOthersOweYou || []).length === 0 ? (
                    <div className="empty-state" style={{ padding: '48px 24px' }}>
                      <div className="empty-state-icon-circle">
                        <Icons.Receipt />
                      </div>
                      <p className="empty-state-title">No pending collections</p>
                      <p className="empty-state-desc">
                        When you pay for a group expense and split it with trip companions, their shares appear here.
                      </p>
                    </div>
                  ) : (
                    <div style={{ overflowX: 'auto' }}>
                      <table className="data-table">
                        <thead>
                          <tr>
                            <th>Expense</th>
                            <th>Trip</th>
                            <th>Owed By</th>
                            <th className="text-right">Amount</th>
                            <th>Status</th>
                            <th className="text-right">Action</th>
                          </tr>
                        </thead>
                        <tbody>
                          {dashboard.whatOthersOweYou.map((item) => (
                            <tr key={item.splitId}>
                              <td style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                                {item.expenseDescription}
                              </td>
                              <td>{item.tripTitle}</td>
                              <td>{item.debtorUserName}</td>
                              <td className="text-right" style={{ fontWeight: 700 }}>
                                {formatCurrency(item.amountOwed)}
                              </td>
                              <td>
                                <span
                                  className={`badge ${
                                    item.isPaid ? 'badge-success' : 'badge-warning'
                                  }`}
                                >
                                  {item.isPaid ? <Icons.Check /> : <Icons.Clock />}
                                  <span>{item.status}</span>
                                </span>
                              </td>
                              <td className="text-right">
                                {!item.isPaid ? (
                                  <button
                                    type="button"
                                    className="btn btn-success btn-sm"
                                    disabled={settlingSplitId === item.splitId}
                                    onClick={() => handleSettleSplit(item.splitId)}
                                  >
                                    {settlingSplitId === item.splitId ? 'Settling...' : 'Mark Received'}
                                  </button>
                                ) : (
                                  <span style={{ fontSize: '0.8rem', color: 'var(--success-text)', fontWeight: 600 }}>
                                    Settled
                                  </span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}

              {/* Tab 2: Expenses You Owe */}
              {dashboardSubTab === 'payables' && (
                <div>
                  {(dashboard?.whatYouOwe || []).length === 0 ? (
                    <div className="ledger-zero-state">
                      <div className="zero-state-banner">
                        <h3 className="zero-state-title">You Have No Outstanding Debts</h3>
                        <p className="zero-state-desc">
                          You do not owe money to any travel companions. You were the payer for your trip expenses, or all your shares have been settled.
                        </p>
                      </div>

                      {/* Informative Recent Group Settlements Table */}
                      <div className="ledger-preview-section">
                        <div className="preview-section-header">
                          <div>
                            <h4 className="preview-title">Completed Settlements Ledger</h4>
                            <p className="preview-subtitle">Recent transactions settled between you and companions</p>
                          </div>
                          <span className="badge badge-success">
                            <Icons.Check />
                            <span>{(dashboard?.allBalances || []).length} Settled</span>
                          </span>
                        </div>

                        {(dashboard?.allBalances || []).length > 0 ? (
                          <div style={{ overflowX: 'auto' }}>
                            <table className="data-table">
                              <thead>
                                <tr>
                                  <th>Expense</th>
                                  <th>Trip</th>
                                  <th>Paid By</th>
                                  <th>Shared With</th>
                                  <th className="text-right">Amount</th>
                                  <th>Status</th>
                                </tr>
                              </thead>
                              <tbody>
                                {(dashboard.allBalances || []).map((item) => (
                                  <tr key={item.splitId}>
                                    <td style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                                      {item.expenseDescription}
                                    </td>
                                    <td>{item.tripTitle}</td>
                                    <td>{item.paidByUserName}</td>
                                    <td>{item.debtorUserName}</td>
                                    <td className="text-right" style={{ fontWeight: 700 }}>
                                      {formatCurrency(item.amountOwed)}
                                    </td>
                                    <td>
                                      <span className="badge badge-success">
                                        <Icons.Check />
                                        <span>Settled</span>
                                      </span>
                                    </td>
                                  </tr>
                                ))}
                              </tbody>
                            </table>
                          </div>
                        ) : (
                          <p style={{ color: 'var(--text-muted)', fontSize: '0.85rem', margin: '16px 0 0' }}>
                            No transactions recorded yet.
                          </p>
                        )}
                      </div>
                    </div>
                  ) : (
                    <div style={{ overflowX: 'auto' }}>
                      <table className="data-table">
                        <thead>
                          <tr>
                            <th>Expense</th>
                            <th>Trip</th>
                            <th>Paid By</th>
                            <th className="text-right">Amount Owed</th>
                            <th>Status</th>
                            <th className="text-right">Action</th>
                          </tr>
                        </thead>
                        <tbody>
                          {dashboard.whatYouOwe.map((item) => (
                            <tr key={item.splitId}>
                              <td style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                                {item.expenseDescription}
                              </td>
                              <td>{item.tripTitle}</td>
                              <td>{item.paidByUserName}</td>
                              <td className="text-right" style={{ fontWeight: 700 }}>
                                {formatCurrency(item.amountOwed)}
                              </td>
                              <td>
                                <span
                                  className={`badge ${
                                    item.isPaid ? 'badge-success' : 'badge-warning'
                                  }`}
                                >
                                  {item.isPaid ? <Icons.Check /> : <Icons.Clock />}
                                  <span>{item.status}</span>
                                </span>
                              </td>
                              <td className="text-right">
                                {!item.isPaid ? (
                                  <button
                                    type="button"
                                    className="btn btn-success btn-sm"
                                    disabled={settlingSplitId === item.splitId}
                                    onClick={() => handleSettleSplit(item.splitId)}
                                  >
                                    {settlingSplitId === item.splitId ? 'Settling...' : 'Mark Settled'}
                                  </button>
                                ) : (
                                  <span style={{ fontSize: '0.8rem', color: 'var(--success-text)', fontWeight: 600 }}>
                                    Settled
                                  </span>
                                )}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}

              {/* Tab 3: Full Settlement Ledger */}
              {dashboardSubTab === 'history' && (
                <div>
                  {(dashboard?.allBalances || []).length === 0 ? (
                    <div className="empty-state" style={{ padding: '48px 24px' }}>
                      <div className="empty-state-icon-circle">
                        <Icons.Receipt />
                      </div>
                      <p className="empty-state-title">No transactions recorded</p>
                      <p className="empty-state-desc">
                        Create a trip and log expenses to start populating your ledger.
                      </p>
                    </div>
                  ) : (
                    <div style={{ overflowX: 'auto' }}>
                      <table className="data-table">
                        <thead>
                          <tr>
                            <th>Expense</th>
                            <th>Trip</th>
                            <th>Paid By</th>
                            <th>Shared With</th>
                            <th className="text-right">Amount</th>
                            <th>Status</th>
                          </tr>
                        </thead>
                        <tbody>
                          {(dashboard.allBalances || []).map((item) => (
                            <tr key={item.splitId}>
                              <td style={{ fontWeight: 600, color: 'var(--text-main)' }}>
                                {item.expenseDescription}
                              </td>
                              <td>{item.tripTitle}</td>
                              <td>{item.paidByUserName}</td>
                              <td>{item.debtorUserName}</td>
                              <td className="text-right" style={{ fontWeight: 700 }}>
                                {formatCurrency(item.amountOwed)}
                              </td>
                              <td>
                                <span
                                  className={`badge ${
                                    item.isPaid ? 'badge-success' : 'badge-warning'
                                  }`}
                                >
                                  {item.isPaid ? <Icons.Check /> : <Icons.Clock />}
                                  <span>{item.status}</span>
                                </span>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        ) : (
          <div>
            <div className="page-header-row">
              <div>
                <div className="page-title-badge">
                  <Icons.Suitcase />
                  <span>Travel Itineraries</span>
                </div>
                <h1 className="page-title">Group Trips</h1>
                <p className="page-subtitle">
                  Create and manage trips, add companions, and split group costs
                </p>
              </div>
              <button
                className="btn btn-primary"
                onClick={() => setShowCreateTrip(!showCreateTrip)}
              >
                <Icons.Plus />
                <span>{showCreateTrip ? 'Cancel' : 'Create Trip'}</span>
              </button>
            </div>

            {tripError && (
              <div className="alert alert-error" style={{ marginBottom: 20 }}>
                {tripError}
              </div>
            )}

            {showCreateTrip && (
              <div className="card" style={{ marginBottom: 28 }}>
                <div className="card-header">
                  <h2 className="card-title">Create New Trip</h2>
                </div>
                <form onSubmit={handleCreateTrip} className="form-stack">
                  <div className="form-group">
                    <label className="form-label">Trip Title</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="e.g. Udaipur Strategic Getaway"
                      value={newTripTitle}
                      onChange={(e) => setNewTripTitle(e.target.value)}
                      required
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">Description (Optional)</label>
                    <input
                      type="text"
                      className="form-input"
                      placeholder="e.g. Executive planning, resort stay, and local dining"
                      value={newTripDesc}
                      onChange={(e) => setNewTripDesc(e.target.value)}
                    />
                  </div>

                  <div className="form-group">
                    <label className="form-label">
                      Select Travel Companions ({newTripMembers.length} selected)
                    </label>
                    <div className="participant-chips">
                      {allUsers
                        .filter((u) => u.id !== currentUserId)
                        .map((u) => {
                          const isSelected = newTripMembers.includes(u.id);
                          return (
                            <button
                              key={u.id}
                              type="button"
                              className={`participant-chip ${isSelected ? 'active' : ''}`}
                              onClick={() => toggleTripMemberSelection(u.id)}
                            >
                              <span className="chip-avatar">{u.name.charAt(0).toUpperCase()}</span>
                              <span>{u.name}</span>
                              {isSelected && <Icons.Check />}
                            </button>
                          );
                        })}
                    </div>
                  </div>

                  <div style={{ display: 'flex', gap: 10, justifyContent: 'flex-end', paddingTop: 8 }}>
                    <button
                      type="button"
                      className="btn btn-secondary"
                      onClick={() => setShowCreateTrip(false)}
                    >
                      Cancel
                    </button>
                    <button type="submit" className="btn btn-primary">
                      Create Trip
                    </button>
                  </div>
                </form>
              </div>
            )}

            {tripsLoading ? (
              <p style={{ color: 'var(--text-muted)' }}>Loading trips...</p>
            ) : trips.length === 0 ? (
              <div className="card empty-state">
                <Icons.Trips />
                <p className="empty-state-title">No trips found</p>
                <p className="empty-state-desc">
                  Create your first group trip to start adding members and tracking shared expenses.
                </p>
              </div>
            ) : (
              <div className="trips-grid">
                {trips.map((trip) => (
                  <div key={trip.id} className="trip-card" onClick={() => openTrip(trip.id)}>
                    <div className="trip-card-top">
                      <div className="trip-card-tag">
                        <Icons.Plane />
                        <span>Active Trip</span>
                      </div>
                      <span className="trip-members-pill">
                        <Icons.Users />
                        <span>
                          {trip.members?.length || 0} {trip.members?.length === 1 ? 'member' : 'members'}
                        </span>
                      </span>
                    </div>

                    <div className="trip-card-body">
                      <h3 className="trip-card-title">{trip.title}</h3>
                      <p className="trip-card-desc">
                        {trip.description || 'No description provided for this trip.'}
                      </p>
                    </div>

                    <div className="trip-card-footer">
                      <div className="trip-organizer">
                        <span className="organizer-avatar">
                          {(trip.createdByUserName || 'H').charAt(0).toUpperCase()}
                        </span>
                        <span className="organizer-name">
                          {trip.createdByUserName ? `By ${trip.createdByUserName}` : 'By Host'}
                        </span>
                      </div>
                      <div className="trip-expense-count">
                        <Icons.Receipt />
                        <span>{trip.expenses?.length || 0} expenses</span>
                      </div>
                      <span className="trip-view-arrow">→</span>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </main>
    </div>
  );
}
