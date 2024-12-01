import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url); // get the resolved path to the file
const __dirname = path.dirname(__filename); // get the name of the directory

const conf = JSON.parse(fs.readFileSync(path.join(__dirname, '../../config.json')).toString());
export default conf;