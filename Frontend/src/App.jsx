import { useState, useEffect } from 'react';

function getUserIdFromToken(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return (
      payload.nameid ||
      payload.sub ||
      payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
    );
  } catch {
    return null;
  }
}

const errorStyle = { color: '#ff6b6b', margin: '8px 0 0' };

function App() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [token, setToken] = useState(localStorage.getItem('token') || '');
  const [groups, setGroups] = useState([]);
  const [groupsError, setGroupsError] = useState('');
  const [selectedGroup, setSelectedGroup] = useState(null);
  const [selectedGroupError, setSelectedGroupError] = useState('');
  const [expenses, setExpenses] = useState([]);
  const [settleUp, setSettleUp] = useState([]);
  const [newDesc, setNewDesc] = useState('');
  const [newAmount, setNewAmount] = useState('');
  const [newPaidBy, setNewPaidBy] = useState('');
  const [newParticipants, setNewParticipants] = useState([]);
  const [expenseError, setExpenseError] = useState('');
  const [newGroupName, setNewGroupName] = useState('');
  const [createGroupError, setCreateGroupError] = useState('');
  const [newMemberId, setNewMemberId] = useState('');
  const [memberError, setMemberError] = useState('');
  const [name, setName] = useState('');
  const [isRegistering, setIsRegistering] = useState(false);
  const [info, setInfo] = useState('');

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

  const handleRegister = async (e) => {
  e.preventDefault();
  setError('');
  setInfo('');

  try {
    const response = await fetch('http://localhost:5033/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ name, email, password }),
    });

    if (!response.ok) {
      const msg = await response.text();
      throw new Error(msg || 'Registration failed');
    }

    setIsRegistering(false);
    setPassword('');
    setInfo('Account created. You can log in now.');
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
    setSettleUp([]);
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

  const loadSettleUp = async (groupId) => {
    try {
      const res = await fetch(`http://localhost:5033/api/expenses/settle-up/${groupId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      setSettleUp(res.ok ? await res.json() : []);
    } catch (err) {
      console.error('Failed to load settle-up:', err);
      setSettleUp([]);
    }
  };

  const recordSettlement = async (s) => {
    try {
      const res = await fetch('http://localhost:5033/api/settlements', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          fromUserId: s.fromUserId,
          toUserId: s.toUserId,
          groupId: selectedGroup.id,
          amount: s.amount,
        }),
      });
      if (!res.ok) throw new Error('Failed to record settlement');
      await loadSettleUp(selectedGroup.id);
    } catch (err) {
      console.error(err);
      setSelectedGroupError(err.message);
    }
  };

  const loadExpenses = async (groupId) => {
    try {
      const res = await fetch(`http://localhost:5033/api/expenses/group/${groupId}`, {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (res.ok) setExpenses(await res.json());
    } catch (err) {
      console.error('Failed to load expenses:', err);
    }
  };

  const toggleParticipant = (id) => {
    setNewParticipants((prev) =>
      prev.includes(id) ? prev.filter((p) => p !== id) : [...prev, id]
    );
  };

  const addExpense = async (e) => {
    e.preventDefault();
    setExpenseError('');

    if (newParticipants.length === 0) {
      setExpenseError('Pick at least one participant.');
      return;
    }

    try {
      const res = await fetch('http://localhost:5033/api/expenses', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({
          description: newDesc,
          amount: Number(newAmount),
          paidByUserId: Number(newPaidBy),
          groupId: selectedGroup.id,
          participantUserIds: newParticipants,
        }),
      });
      if (!res.ok) throw new Error('Failed to add expense (check description and amount)');

      setNewDesc('');
      setNewAmount('');
      await loadExpenses(selectedGroup.id);
      await loadSettleUp(selectedGroup.id);
    } catch (err) {
      setExpenseError(err.message);
    }
  };

  const createGroup = async (e) => {
    e.preventDefault();
    setCreateGroupError('');

    try {
      const res = await fetch('http://localhost:5033/api/groups', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ name: newGroupName }),
      });
      if (!res.ok) throw new Error('Failed to create group');

      const created = await res.json();
      setGroups((prev) => [...prev, created]);
      setNewGroupName('');
    } catch (err) {
      setCreateGroupError(err.message);
    }
  };

  const addMember = async (e) => {
    e.preventDefault();
    setMemberError('');

    try {
      const res = await fetch(`http://localhost:5033/api/groups/${selectedGroup.id}/members`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify({ userId: Number(newMemberId) }),
      });
      if (!res.ok) {
        const msg = await res.text();
        throw new Error(msg || 'Failed to add member');
      }
      setNewMemberId('');
      await openGroup(selectedGroup.id);
    } catch (err) {
      setMemberError(err.message);
    }
  };

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

      const memberList = (data.members || []).filter(Boolean);
      setNewPaidBy(memberList[0] ? String(memberList[0].id) : '');
      setNewParticipants(memberList.map((m) => m.id));
      setExpenseError('');
    } catch (err) {
      setSelectedGroupError(err.message);
      setSelectedGroup(null);
      return;
    }

    await loadExpenses(groupId);
    await loadSettleUp(groupId);
  };

  // ---------- Group detail view ----------
  if (token && selectedGroup) {
    return (
      <div>
        <button className="secondary" onClick={() => setSelectedGroup(null)}>
          ← Back to Groups
        </button>

        <h1>{selectedGroup.name}</h1>

        {selectedGroupError && <p style={errorStyle}>{selectedGroupError}</p>}

        <div className="card">
          <h2>Members</h2>
          <ul>
            {selectedGroup.members?.map((m) =>
              m ? (
                <li key={m.id}>
                  <span>{m.name}</span>
                  <span className="muted">ID {m.id}</span>
                </li>
              ) : null
            )}
          </ul>

          <form onSubmit={addMember} style={{ marginTop: 12 }}>
            <div className="row">
              <input
                type="number"
                min="1"
                placeholder="User ID to add"
                value={newMemberId}
                onChange={(e) => setNewMemberId(e.target.value)}
                required
              />
              <button type="submit">Add Member</button>
            </div>
            {memberError && <p style={errorStyle}>{memberError}</p>}
          </form>
        </div>

        <div className="card">
          <h2>Expenses</h2>
          {expenses.length === 0 ? (
            <p className="muted" style={{ margin: 0 }}>No expenses yet.</p>
          ) : (
            <ul>
              {expenses.map((exp) => (
                <li key={exp.id}>
                  <span>
                    {exp.description}
                    <span className="muted"> · paid by {exp.paidByUser?.name || 'Unknown'}</span>
                  </span>
                  <strong>₹{exp.amount}</strong>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="card">
          <h2>Add Expense</h2>
          <form onSubmit={addExpense} className="stack">
            <input
              placeholder="Description"
              value={newDesc}
              onChange={(e) => setNewDesc(e.target.value)}
              required
            />
            <input
              type="number"
              step="0.01"
              min="0.01"
              placeholder="Amount"
              value={newAmount}
              onChange={(e) => setNewAmount(e.target.value)}
              required
            />

            <div>
              <label className="muted" style={{ marginBottom: 6 }}>Paid by</label>
              <select value={newPaidBy} onChange={(e) => setNewPaidBy(e.target.value)}>
                {selectedGroup.members?.filter(Boolean).map((m) => (
                  <option key={m.id} value={m.id}>{m.name}</option>
                ))}
              </select>
            </div>

            <div>
              <label className="muted" style={{ marginBottom: 6 }}>Split between</label>
              {selectedGroup.members?.filter(Boolean).map((m) => (
                <label key={m.id} style={{ padding: '4px 0' }}>
                  <input
                    type="checkbox"
                    checked={newParticipants.includes(m.id)}
                    onChange={() => toggleParticipant(m.id)}
                  />
                  {m.name}
                </label>
              ))}
            </div>

            <button type="submit">Add Expense</button>
            {expenseError && <p style={errorStyle}>{expenseError}</p>}
          </form>
        </div>

        <div className="card">
          <h2>Settle Up</h2>
          {settleUp.length === 0 ? (
            <p className="muted" style={{ margin: 0 }}>All settled up 🎉</p>
          ) : (
            <ul>
              {settleUp.map((s, i) => (
                <li key={`${s.fromUserId}-${s.toUserId}-${i}`}>
                  <span>
                    {s.fromUserName} owes {s.toUserName}{' '}
                    <strong>₹{s.amount.toFixed(2)}</strong>
                  </span>
                  <button onClick={() => recordSettlement(s)}>Mark as paid</button>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    );
  }

  // ---------- Groups list view ----------
  if (token) {
    return (
      <div>
        <h1>Your Groups</h1>
        <p className="muted" style={{ textAlign: 'center', margin: '0 0 16px' }}>
          Your user ID: {getUserIdFromToken(token)}
        </p>

        <div className="card">
          <form onSubmit={createGroup}>
            <div className="row">
              <input
                placeholder="New group name"
                value={newGroupName}
                onChange={(e) => setNewGroupName(e.target.value)}
                required
              />
              <button type="submit">Create Group</button>
            </div>
            {createGroupError && <p style={errorStyle}>{createGroupError}</p>}
          </form>
        </div>

        {groupsError && <p style={errorStyle}>{groupsError}</p>}

        <div className="card">
          {groups.length === 0 && !groupsError ? (
            <p className="muted" style={{ margin: 0 }}>No groups yet.</p>
          ) : (
            <ul>
              {groups.map((group) => (
                <li
                  key={group.id}
                  onClick={() => openGroup(group.id)}
                  style={{ cursor: 'pointer' }}
                >
                  <span>{group.name}</span>
                  <span className="muted">›</span>
                </li>
              ))}
            </ul>
          )}
        </div>

        <button className="secondary" onClick={handleLogout}>
          Logout
        </button>
      </div>
    );
  }

  // ---------- Login view ----------
  return (
  <div>
    <h1>{isRegistering ? 'Create Account' : 'Login'}</h1>

    <div className="card">
      <form onSubmit={isRegistering ? handleRegister : handleLogin} className="stack">
        {isRegistering && (
          <div>
            <label className="muted" style={{ marginBottom: 6 }}>Name</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required />
          </div>
        )}

        <div>
          <label className="muted" style={{ marginBottom: 6 }}>Email</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div>
          <label className="muted" style={{ marginBottom: 6 }}>Password</label>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        <button type="submit">{isRegistering ? 'Register' : 'Login'}</button>
      </form>

      {info && <p style={{ color: '#6bdc8b', margin: '8px 0 0' }}>{info}</p>}
      {error && <p style={errorStyle}>{error}</p>}

      <p className="muted" style={{ marginBottom: 0 }}>
        {isRegistering ? 'Already have an account? ' : "Don't have an account? "}
        <a
          href="#"
          onClick={(e) => {
            e.preventDefault();
            setIsRegistering(!isRegistering);
            setError('');
            setInfo('');
          }}
        >
          {isRegistering ? 'Login' : 'Register'}
        </a>
      </p>
    </div>
  </div>
)
};

export default App;