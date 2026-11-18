# TripSplit — Frontend Client

A responsive single-page web client built with **React 19** and **Vite** for the Group Travel Expense Splitter platform.

---

## Technical Overview

- **Framework**: React 19 (`react`, `react-dom`)
- **Build Tool**: Vite 8 with Hot Module Replacement (HMR)
- **Styling**: Pure CSS3 design system with CSS custom properties (light enterprise palette, responsive layout, accessible typography)
- **State Management**: React hooks (`useState`, `useEffect`, `useCallback`)
- **API Integration**: Native `fetch` with JWT Bearer authentication headers

---

## Project Structure

```
Frontend/
├── public/
│   └── favicon.svg         # SVG brand favicon
├── src/
│   ├── App.jsx             # Main application orchestrator & views
│   ├── App.css             # Component layout, cards, modals & styles
│   ├── index.css           # Global typography and base CSS reset
│   └── main.jsx            # Application root mounting point
├── index.html              # HTML entry template
├── package.json            # Node dependencies and build scripts
└── vite.config.js          # Vite build configuration
```

---

## Available Scripts

In the `Frontend` directory, you can run:

### `npm run dev`
Runs the development server at **`http://localhost:5173`** with hot module replacement enabled.

### `npm run build`
Compiles the React application into production-ready static assets in the `dist/` directory.

### `npm run preview`
Locally previews the production build.

### `npm run lint`
Runs ESLint across all source files to verify code quality.

---

## Connecting to Backend

The frontend communicates with the ASP.NET Core Web API backend via:
```javascript
const API_BASE = 'http://localhost:5033/api';
```
Ensure the backend service is running on `http://localhost:5033` before starting the frontend development server.
