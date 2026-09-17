const fs = require('fs');
const data = JSON.parse(fs.readFileSync('eslint-output.json'));
let total = 0;
data.forEach(d => {
  if (d.messages.length > 0) {
    console.log('\n--- ' + d.filePath);
    d.messages.forEach(m => {
      console.log(`L${m.line}:${m.column} -> ${m.message}`);
      total++;
    });
  }
});
console.log('\nTotal literal strings found: ' + total);
