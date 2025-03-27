import{r as n,o as a,c as r,b as t,a as o,F as d,d as e,e as l}from"./app.b09da964.js";import{_ as i}from"./plugin-vue_export-helper.21dcd24c.js";const c={},p=e('<h1 id="configuration" tabindex="-1"><a class="header-anchor" href="#configuration" aria-hidden="true">#</a> Configuration</h1><p>Each task in <code>task-runner.json</code> is a JSON object with the following properties:</p><table><thead><tr><th>name</th><th>optional</th><th>type</th><th>default</th><th>description</th></tr></thead><tbody><tr><td>command</td><td>false</td><td>string</td><td>-</td><td>path to the executable file or script or executable name</td></tr><tr><td>args</td><td>true</td><td>[string array]</td><td>-</td><td>command arguments</td></tr><tr><td>include</td><td>true</td><td>[array of glob]</td><td><code>**/*</code></td><td>glob pattern to select files</td></tr><tr><td>name</td><td>true</td><td>string</td><td>command</td><td>name of the task (recommended)</td></tr><tr><td>group</td><td>true</td><td>string</td><td>-</td><td>group of the task (usually it should be the hook name)</td></tr><tr><td>branch</td><td>true</td><td>string (regex)</td><td>-</td><td>run task on specific branches only</td></tr><tr><td>pathMode</td><td>true</td><td>[absolute, relative]</td><td>relative</td><td>file path style (relative or absolute)</td></tr><tr><td>cwd</td><td>true</td><td>string</td><td>project root directory</td><td>current working directory for the command, can be relative or absolute</td></tr><tr><td>output</td><td>true</td><td>[always, verbose, never]</td><td>always</td><td>output log level</td></tr><tr><td>exclude</td><td>true</td><td>[array of glob]</td><td>-</td><td>glob pattern to exclude files</td></tr><tr><td>filteringRule</td><td>true</td><td>[variable, staged]</td><td>variable</td><td>match include/exclude against the used variables or git staged files</td></tr><tr><td>windows</td><td>true</td><td>object</td><td>-</td><td>overrides all the above settings for windows</td></tr></tbody></table><h2 id="glob-patterns" tabindex="-1"><a class="header-anchor" href="#glob-patterns" aria-hidden="true">#</a> Glob patterns</h2><p>Husky.Net supports the standard dotnet <code>FileSystemGlobbing</code> patterns for include or exclude task configurations. The patterns that are specified in the <code>include</code> and <code>exclude</code> can use the following formats to match multiple files or directories.</p><ul><li>Exact directory or file name <ul><li>some-file.txt</li><li>path/to/file.txt</li></ul></li><li>Wildcards * in file and directory names that represent zero to many characters not including separator characters.</li></ul><table><thead><tr><th>Value</th><th>Description</th></tr></thead><tbody><tr><td>*.txt</td><td>All files with .txt file extension.</td></tr><tr><td><em>.</em></td><td>All files with an extension.</td></tr><tr><td>*</td><td>All files in top-level directory.</td></tr><tr><td>.*</td><td>File names beginning with &#39;.&#39;.</td></tr><tr><td><em>word</em></td><td>All files with &#39;word&#39; in the filename.</td></tr><tr><td>readme.*</td><td>All files named &#39;readme&#39; with any file extension.</td></tr><tr><td>styles/*.css</td><td>All files with extension &#39;.css&#39; in the directory &#39;styles/&#39;.</td></tr><tr><td>scripts/<em>/</em></td><td>All files in &#39;scripts/&#39; or one level of subdirectory under &#39;scripts/&#39;.</td></tr><tr><td>images*/*</td><td>All files in a folder with name that is or begins with &#39;images&#39;.</td></tr></tbody></table><ul><li>Arbitrary directory depth (/**/).</li></ul><table><thead><tr><th>Value</th><th>Description</th></tr></thead><tbody><tr><td>*<em>/</em></td><td>All files in any subdirectory.</td></tr><tr><td>dir/**/*</td><td>All files in any subdirectory under &#39;dir/&#39;.</td></tr></tbody></table><ul><li>Relative paths.</li></ul><p>To match all files in a directory named &quot;shared&quot; at the sibling level to the base directory use <code>../shared/*</code>.</p>',11),u={href:"https://docs.microsoft.com/en-us/dotnet/core/extensions/file-globbing#pattern-formats",target:"_blank",rel:"noopener noreferrer"},h=l("Read more here"),b=e(`<h2 id="variables" tabindex="-1"><a class="header-anchor" href="#variables" aria-hidden="true">#</a> Variables</h2><p>There are some variables that you can use in your task arguments (<code>args</code>).</p><ul><li><strong>\${staged}</strong><ul><li>returns the list of currently staged files</li></ul></li><li><strong>\${last-commit}</strong><ul><li>returns last commit changed files</li></ul></li><li><strong>\${git-files}</strong><ul><li>returns the output of (git ls-files)</li></ul></li><li><strong>\${all-files}</strong><ul><li>returns the list of matched files using include/exclude, be careful with this variable, it will return all the files if you don&#39;t specify include or exclude</li></ul></li><li><strong>\${args}</strong><ul><li>returns the arguments passed directly to the <code>husky run</code> command using <code>--args</code> option</li></ul></li></ul><p>e.g.</p><div class="language-json ext-json"><pre class="language-json"><code><span class="token property">&quot;args&quot;</span><span class="token operator">:</span> <span class="token punctuation">[</span> <span class="token string">&quot;\${staged}&quot;</span> <span class="token punctuation">]</span>
</code></pre></div><h3 id="custom-variables" tabindex="-1"><a class="header-anchor" href="#custom-variables" aria-hidden="true">#</a> Custom variables</h3><p>You can define your own variables by adding a task to the <code>variables</code> section in <code>task-runner.json</code>.</p><p>e.g.</p><p>defining custom <code>\${root-dir-files}</code> variable to access root directory files</p><div class="language-json ext-json line-numbers-mode"><pre class="language-json"><code><span class="token punctuation">{</span>
   <span class="token property">&quot;variables&quot;</span><span class="token operator">:</span> <span class="token punctuation">[</span>
      <span class="token punctuation">{</span>
         <span class="token property">&quot;name&quot;</span><span class="token operator">:</span> <span class="token string">&quot;root-dir-files&quot;</span><span class="token punctuation">,</span>
         <span class="token property">&quot;command&quot;</span><span class="token operator">:</span> <span class="token string">&quot;cmd&quot;</span><span class="token punctuation">,</span>
         <span class="token property">&quot;args&quot;</span><span class="token operator">:</span> <span class="token punctuation">[</span><span class="token string">&quot;/c&quot;</span><span class="token punctuation">,</span> <span class="token string">&quot;dir&quot;</span><span class="token punctuation">,</span> <span class="token string">&quot;/b&quot;</span><span class="token punctuation">]</span>
      <span class="token punctuation">}</span>
   <span class="token punctuation">]</span><span class="token punctuation">,</span>
   <span class="token property">&quot;tasks&quot;</span><span class="token operator">:</span> <span class="token punctuation">[</span>
      <span class="token punctuation">{</span>
         <span class="token property">&quot;command&quot;</span><span class="token operator">:</span> <span class="token string">&quot;cmd&quot;</span><span class="token punctuation">,</span>
         <span class="token property">&quot;args&quot;</span><span class="token operator">:</span> <span class="token punctuation">[</span><span class="token string">&quot;/c&quot;</span><span class="token punctuation">,</span> <span class="token string">&quot;echo&quot;</span><span class="token punctuation">,</span> <span class="token string">&quot;\${root-dir-files}&quot;</span><span class="token punctuation">]</span>
      <span class="token punctuation">}</span>
   <span class="token punctuation">]</span>
<span class="token punctuation">}</span>
</code></pre><div class="line-numbers"><span class="line-number">1</span><br><span class="line-number">2</span><br><span class="line-number">3</span><br><span class="line-number">4</span><br><span class="line-number">5</span><br><span class="line-number">6</span><br><span class="line-number">7</span><br><span class="line-number">8</span><br><span class="line-number">9</span><br><span class="line-number">10</span><br><span class="line-number">11</span><br><span class="line-number">12</span><br><span class="line-number">13</span><br><span class="line-number">14</span><br><span class="line-number">15</span><br></div></div>`,10);function m(g,f){const s=n("ExternalLinkIcon");return a(),r(d,null,[p,t("p",null,[t("a",u,[h,o(s)])]),b],64)}var q=i(c,[["render",m]]);export{q as default};
