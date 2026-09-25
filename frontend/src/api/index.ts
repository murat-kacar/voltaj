// V2: Single entry point for all API access.
// Principle 2 (Continue): existing imports from 'src/api.ts' continue to work unchanged —
// api.ts is now a re-export barrel pointing to the modular api/ directory.

export * from './_base'
export * from './auth'
export * from './customers'
export * from './sales'
export * from './inventory'
export * from './finance'
export * from './services'
export * from './other'
