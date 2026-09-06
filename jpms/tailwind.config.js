/** @type {import('tailwindcss').Config} */
//
// The tokens are the Open Book Figma's local styles, read out of the file on 2026-09-03 —
// docs/ui/open-book-design-rules.md §1–2 is the walk that produced every value here and the
// place to look before changing one. Alpha styles are flattened over the surface they sit on in
// the file (Tailwind's /90 modifier only works on solid hexes). Where the Figma and an earlier
// jpms rule disagree, the Figma wins (James, 2026-09-03).
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
      // The Figma type scale: eight sizes, each with a FIXED leading tighter than Tailwind's
      // defaults (14/16 not 14/20, 16/20 not 16/24…). Overriding the named sizes means every
      // existing text-sm / text-base picks up the design's leading without markup churn.
      // Hierarchy is carried by weight, not size; nothing in the design is smaller than 12px.
      fontSize: {
        xs:    ['12px', { lineHeight: '14px' }],
        sm:    ['14px', { lineHeight: '16px' }],
        base:  ['16px', { lineHeight: '20px' }],
        lg:    ['18px', { lineHeight: '22px' }],
        xl:    ['20px', { lineHeight: '20px' }],
        '2xl': ['24px', { lineHeight: '24px' }],
        '4xl': ['40px', { lineHeight: '40px' }],
        '5xl': ['48px', { lineHeight: '48px' }]
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
        // BG/Dark — the page.
        canvas: '#101111',
        surface: {
          // Panels/Dark @90% over canvas — chrome, cards, table bodies.
          DEFAULT: '#19191C',
          // Panels/Table Highlight @70% over surface — ONE Figma style behind both names:
          // `raised` for hover/selected rows, `field` for input fills. Same hex on purpose.
          raised: '#282A31',
          field: '#282A31'
        },
        line: {
          // Boarders/Outline — structural: nav, top bar, cards, SMALL buttons.
          DEFAULT: '#2E323A',
          // Boarders/Table Seperator @80% over surface — anything row- or field-shaped: table
          // cells, inputs, radios, checkboxes, the modal, LARGE buttons.
          strong: '#3C414A'
        },
        content: {
          // Text/White — titles, values, button labels, header cells (G2 nav hover folds in here).
          DEFAULT: '#FFFFFF',
          // Text/G4 — table body text (G3 card icons fold in here).
          muted: '#DDDDDD',
          // Text/G5 — labels and captions. Much lighter than before, deliberately: a label is
          // content, not a hint.
          subtle: '#D1D1D1',
          // Text/G6 — inactive nav, placeholders, muted buttons, the "+" tile.
          faint: '#8C8C8C'
        },
        accent: {
          // Status/Positive — the ONE solid-green primary control per view. Text on it is the
          // canvas colour.
          DEFAULT: '#66E094',
          // Not a Figma style (the Button component's Hover variant is still to be read —
          // open-book-design-rules.md §8). A shade darker so a press reads as a press.
          hover: '#5ACF85',
          ink: '#101111'
        },
        // The same green as accent: the file uses one style for the primary fill and for
        // positive deltas.
        positive: '#66E094',
        negative: {
          // Status/Negative — a negative figure in a table, an error. Never a button fill.
          // `strong` is the fill behind the error toast, dark enough for near-white text to clear
          // WCAG AA on top of it; `strong`/`ink` are jpms's own, not in the Figma.
          DEFAULT: '#FF403C',
          strong: '#8E1616',
          ink: '#FFF1F1'
        },
        // Status/Neutral — neutral status AND the link colour inside tables. Never an action.
        info: '#3CA1FF',
        // Brand/Main — the logo / brand mark only. It is NOT the action colour.
        brand: '#4CDBEE'
      }
    }
  },
  plugins: []
};
