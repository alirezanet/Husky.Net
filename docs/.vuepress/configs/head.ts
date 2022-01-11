import type { HeadConfig } from 'vuepress-vite';

export const head: HeadConfig[] = [
   // favicon
   ['link', { rel: 'icon', href: 'favicon.ico', type: "image/x-icon" }],
   ['link', { rel: 'shortcut icon', href: 'favicon.ico', type: "image/x-icon" }],

   // social media image
   ['meta', { property: 'og:image', content: 'https://alirezanet.github.io/Husky.Net/workflow.jpg' }],
   ['meta', { property: 'og:image:type', content: 'image/png' }],
   ['meta', { property: 'og:image:width', content: '1280' }],
   ['meta', { property: 'og:image:height', content: '640' }],
   ['meta', { property: 'og:title', content: 'Husky.Net' }],
   ['meta', { property: 'og:type', content: 'website' }],
   ['meta', { property: 'og:url', content: 'https://alirezanet.github.io/Husky.Net/' }],
   ['meta', { property: 'twitter:card', content: 'summary_large_image' }],
]
