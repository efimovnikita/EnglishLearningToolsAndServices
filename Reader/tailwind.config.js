/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./**/*.{razor,html,cshtml}'],
  plugins: [
    require('tailwind-scrollbar')
  ]
}