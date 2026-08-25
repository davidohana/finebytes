import { createRequire } from "node:module";
import { globSync } from "node:fs";

const require = createRequire(import.meta.url);
const { MarkdownTableFormatter } = require("markdown-table-formatter");

/** @returns {string[]} */
export function getMarkdownTablePaths() {
  return globSync("**/*.md", {
    exclude: (path) =>
      path.includes("node_modules") ||
      path.includes(".cursor") ||
      path.includes("/bin/") ||
      path.includes("\\bin\\") ||
      path.includes("/obj/") ||
      path.includes("\\obj\\"),
  });
}

/**
 * @param {{ checkOnly?: boolean }} options
 * @returns {Promise<void>}
 */
export async function runMarkdownTableFormatter(options = {}) {
  const checkOnly = options.checkOnly ?? false;
  const paths = getMarkdownTablePaths();
  if (paths.length === 0) {
    return;
  }

  const formatter = new MarkdownTableFormatter({ check: checkOnly });
  const { status, updates } = await formatter.run(paths);
  if (updates.length > 0) {
    const action = checkOnly ? "need table alignment" : "aligned markdown tables in";
    console.log(`${updates.length} file(s) ${action}:\n- ${updates.join("\n- ")}`);
  }

  if (checkOnly && status !== 0) {
    process.exit(1);
  }
}
