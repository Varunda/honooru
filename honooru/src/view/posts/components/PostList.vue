
<template>
    <div>
        <div style="grid-template-columns: repeat(auto-fill, minmax(180px, 1fr)); display: grid; gap: 0.5rem;">
            <div v-for="post in posts">
                <a :href="'/post/' + post.id" style="max-height: 180px; max-width: 180px;">
                    <img :src="'/media/180x180/' + post.md5 + '.png'" />
                </a>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { Post } from "api/PostApi";

    export const PostList = Vue.extend({
        props: {
            posts: { type: Array as PropType<Post[]>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        components: {

        }

    });
    export default PostList;
</script>