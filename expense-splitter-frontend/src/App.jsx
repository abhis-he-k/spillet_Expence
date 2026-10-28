import { useState, useEffect } from 'react';

function App() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [token, setToken] = useState(localStorage.getItem('token') || '');
  const [groups, setGroups] = useState([]);
  const [groupsError, setGroupsError] = useState('');
  const [selectedGroup, setSelectedGroup] = useState(null);
  const [selectedGroupError, setSelectedGroupError] = useState('');
  const [expenses, setExpenses] = useState([]);   // ADDED — was missing

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');

    try {
      const response = await fetch('http://localhost:5033/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        throw new Error('Invalid email or password');
      }

      const data = await response.json();
      setToken(data.token);
      localStorage.setItem('token', data.token);
    } catch (err) {
      setError(err.message);
    }
  };

  const handleLogout = () => {
    setToken('');
    localStorage.removeItem('token');
    setGroups([]);
    setSelectedGroup(null);
    setExpenses([]);
  };

  useEffect(() => {
    if (!token) return;

    const fetchGroups = async () => {
      setGroupsError('');
      try {
        const response = await fetch('http://localhost:5033/api/groups', {
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!response.ok) {
          throw new Error('Failed to load groups');
        }

        const data = await response.json();
        setGroups(data);
      } catch (err) {
        setGroupsError(err.message);
      }
    };

    fetchGroups();
  }, [token]);

  const openGroup = async (groupId) => {
    setSelectedGroupError('');
    try {
      const response = await fetch(`http://localhost:5033/api/groups/${groupId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });

      if (response.status === 403) {
        throw new Error("You don't have access to this group.");
      }

      if (!response.ok) {
        throw new Error('Failed to load group details');
      }

      const data = await response.json();
      setSelectedGroup(data);
      setSelectedGroupError('');
    } catch (err) {
      setSelectedGroupError(err.message);
      setSelectedGroup(null);
      return;
    }

    // Separate try/catch — a failure here won't wipe out the group we just loaded
    try {
      const expensesResponse = await fetch(`http://localhost:5033/api/expenses/group/${groupId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (expensesResponse.ok) {
        const expensesData = await expensesResponse.json();
        setExpenses(expensesData);
      }
    } catch (err) {
      console.error('Failed to load expenses:', err);
    }
  };

  if (token && selectedGroup) {
    return (
      <div style={{ maxWidth: '500px', margin: '50px auto', fontFamily: 'sans-serif' }}>
        <button onClick={() => setSelectedGroup(null)} style={{ marginBottom: '20px' }}>
          ← Back to Groups
        </button>

        <h1>{selectedGroup.name}</h1>

        {selectedGroupError && <p style={{ color: 'red' }}>{selectedGroupError}</p>}

        <h3>Members</h3>
        <ul>
          {selectedGroup.members?.map((m) =>
            m ? <li key={m.id}>{m.name}</li> : null
          )}
        </ul>

        <h3>Expenses</h3>
        <ul>
          {expenses.map((exp) => (
            <li key={exp.id}>
              {exp.description} — ₹{exp.amount} (paid by {exp.paidByUser?.name || 'Unknown'})
            </li>
          ))}
        </ul>
        {expenses.length === 0 && <p>No expenses yet.</p>}
      </div>
    );
  }

  if (token) {
    return (
      <div style={{ maxWidth: '500px', margin: '50px auto', fontFamily: 'sans-serif' }}>
        <h1>Your Groups</h1>
        <button onClick={handleLogout} style={{ padding: '10px 20px', marginBottom: '20px' }}>
          Logout
        </button>

        {groupsError && <p style={{ color: 'red' }}>{groupsError}</p>}

        <ul>
          {groups.map((group) => (
            <li
              key={group.id}
              onClick={() => openGroup(group.id)}
              style={{ cursor: 'pointer', color: 'lightblue', textDecoration: 'underline' }}
            >
              {group.name}
            </li>
          ))}
        </ul>

        {groups.length === 0 && !groupsError && <p>No groups yet.</p>}
      </div>
    );
  }

  return (
    <div style={{ maxWidth: '400px', margin: '50px auto', fontFamily: 'sans-serif' }}>
      <h1>Login</h1>

      <form onSubmit={handleLogin}>
        <div style={{ marginBottom: '10px' }}>
          <label>Email</label><br />
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={{ width: '100%', padding: '8px' }}
            required
          />
        </div>

        <div style={{ marginBottom: '10px' }}>
          <label>Password</label><br />
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            style={{ width: '100%', padding: '8px' }}
            required
          />
        </div>

        <button type="submit" style={{ padding: '10px 20px' }}>
          Login
        </button>
      </form>

      {error && <p style={{ color: 'red' }}>{error}</p>}
    </div>
  );
}

export default App;