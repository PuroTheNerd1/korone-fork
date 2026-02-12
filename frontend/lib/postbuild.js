import { execSync } from "child_process";

try {
    const buildNum = execSync("git rev-list --count HEAD").toString().trim();

    execSync("git add -A");
    execSync(`git commit -m "Successful build #${buildNum}"`);
    execSync("git push");

    console.log("Committed successful build:", buildNum);
} catch (e) {
    console.error("Commit skipped or failed.");
}
