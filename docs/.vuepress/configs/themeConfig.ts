import type { DefaultThemeOptions } from 'vuepress-vite'
import { sidebar, navbar } from '.'

export const themeConfig: DefaultThemeOptions = {
   editLinks: true,
   editLinkText: 'Help us improve this page!',
   contributors: false,
   docsRepo: 'alirezanet/husky.net',
   docsBranch: 'master',
   docsDir: '/docs',
   repo: 'alirezanet/husky.net',
   sidebar: sidebar,
   navbar: navbar,
}
