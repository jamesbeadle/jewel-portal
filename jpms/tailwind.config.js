/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './**/*.cs',
    './wwwroot/index.html'
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: [
          'Poppins',
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'system-ui',
          'sans-serif'
        ]
      },
      keyframes: {
        'jewel-pulse': {
          '0%, 100%': { color: '#FFFFFF' },
          '50%': { color: '#9AA0A8' }
        }
      },
      animation: {
        'jewel-pulse': 'jewel-pulse 1.6s ease-in-out infinite'
      },
      colors: {
        canvas: '#0B0B0C',
        surface: {
          DEFAULT: '#161719',
          raised: '#1F2024',
          field: '#2A2D31'
        },
        line: {
          DEFAULT: '#232427',
          strong: '#34373B'
        },
        content: {
          DEFAULT: '#FFFFFF',
          muted: '#C4C8CE',
          subtle: '#8A9099',
          faint: '#5A5F68'
        },
        accent: {
          DEFAULT: '#57E08A',
          hover: '#4ECF7E',
          ink: '#0B0B0C'
        },
        positive: '#4ED07D',
        negative: {
          // The bright red stays the DEFAULT: it is the colour of a negative figure in a table,
          // where it sits on the dark canvas and has to stand out. `strong` is the fill behind the
          // error toast, dark enough for near-white text to clear WCAG AA on top of it.
          DEFAULT: '#FF4D4D',
          strong: '#8E1616',
          ink: '#FFF1F1'
        },
        info: '#4691F6'
      }
    }
  },
  plugins: []
};
