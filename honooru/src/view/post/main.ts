
import { createApp } from "vue";

import ViewPost from "view/post/ViewPost.vue";

const app = createApp({
	created: function(): void {

	},

	data: function() {
        return {

        }
	},

	methods: {

	},

	components: {
		ViewPost
	}
	
}).mount("#app");

(window as any).app = app;
