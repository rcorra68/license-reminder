module.exports = {
  extends: ["@commitlint/config-conventional"],
  rules: {
    // Alza il limite a 100 per il titolo (standard moderno)
    "header-max-length": [2, "always", 100],
    // Disabilita il limite sulla lunghezza del corpo o alzalo drasticamente
    "body-max-line-length": [0, "never"],
    // Forza l'uso di tipi validi (feat, fix, docs, style, refactor, perf, test, build, ci, chore, revert)
    "type-enum": [
      2,
      "always",
      [
        "feat",
        "fix",
        "docs",
        "style",
        "refactor",
        "perf",
        "test",
        "build",
        "ci",
        "chore",
        "revert",
      ],
    ],
  },
};
