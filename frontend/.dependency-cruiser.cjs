/** @type {import('dependency-cruiser').IConfiguration} */
module.exports = {
  forbidden: [
    {
      name: 'no-circular',
      severity: 'error',
      comment: 'Circular dependencies cause unexpected behaviors and runtime errors.',
      from: {},
      to: { circular: true }
    },
    {
      name: 'api-layer-isolation',
      severity: 'error',
      comment: 'The api folder should not depend on UI components.',
      from: { path: '^src/api' },
      to: { path: '^src/(common|dashboard|layout|customers|quotes|payments|settings|reminders)' }
    }
  ],
  options: {
    doNotFollow: {
      path: 'node_modules'
    },
    tsConfig: {
      fileName: 'tsconfig.app.json'
    },
    reporterOptions: {
      text: { keepLevels: true }
    }
  }
};
