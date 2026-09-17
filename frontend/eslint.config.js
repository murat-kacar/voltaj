import eslint from '@eslint/js';
import tseslint from 'typescript-eslint';
import reactPlugin from 'eslint-plugin-react';
import i18nextPlugin from 'eslint-plugin-i18next';

export default tseslint.config(
  eslint.configs.recommended,
  ...tseslint.configs.recommended,
  {
    ignores: ['dist', 'node_modules', '*.js', '*.cjs', 'vite.config.ts']
  },
  {
    files: ['src/**/*.{ts,tsx}'],
    languageOptions: {
      parserOptions: {
        ecmaFeatures: {
          jsx: true
        }
      }
    },
    plugins: {
      react: reactPlugin,
      i18next: i18nextPlugin
    },
    rules: {
      'i18next/no-literal-string': [
        'error',
        {
          markupOnly: true,
          ignoreText: ['✓', '✕', '⌕', '⌄', '▤', '◌', '₺', '→', '♧', '◫', '↗', 'voltflow', 'Voltflow Workspace', 'Workspace', 'ID:'],
          ignoreAttribute: ['data-testid', 'className', 'type', 'id', 'name', 'value', 'key', 'path', 'stroke', 'fill', 'xmlns', 'viewBox']
        }
      ],
      '@typescript-eslint/no-explicit-any': 'off',
      '@typescript-eslint/no-unused-vars': 'off'
    }
  }
);
