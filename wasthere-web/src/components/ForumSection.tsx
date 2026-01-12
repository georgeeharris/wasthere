import { useState, useEffect } from 'react';
import { clubNightsApi } from '../services/api';
import type { ClubNightPost, ClubNightPostDto } from '../types';
import { useAuth0 } from '@auth0/auth0-react';
import '../styles/ForumSection.css';

interface ForumSectionProps {
  clubNightId: number;
}

export function ForumSection({ clubNightId }: ForumSectionProps) {
  const [posts, setPosts] = useState<ClubNightPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [newPostContent, setNewPostContent] = useState('');
  const [quotedPost, setQuotedPost] = useState<ClubNightPost | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const { isAuthenticated, loginWithRedirect } = useAuth0();

  useEffect(() => {
    loadPosts();
  }, [clubNightId]);

  const loadPosts = async () => {
    try {
      setLoading(true);
      const data = await clubNightsApi.getPosts(clubNightId);
      setPosts(data);
      setError(null);
    } catch (err) {
      console.error('Failed to load posts:', err);
      setError('Failed to load memories');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!isAuthenticated) {
      loginWithRedirect();
      return;
    }

    if (!newPostContent.trim()) {
      return;
    }

    try {
      setSubmitting(true);
      const dto: ClubNightPostDto = {
        content: newPostContent,
        quotedPostId: quotedPost?.id,
      };
      
      await clubNightsApi.createPost(clubNightId, dto);
      // Refetch posts to ensure proper ordering
      await loadPosts();
      setNewPostContent('');
      setQuotedPost(null);
      setError(null);
    } catch (err) {
      console.error('Failed to create post:', err);
      setError('Failed to post memory');
    } finally {
      setSubmitting(false);
    }
  };

  const handleQuote = (post: ClubNightPost) => {
    setQuotedPost(post);
    setNewPostContent('');
  };

  const handleCancelQuote = () => {
    setQuotedPost(null);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  if (loading) {
    return (
      <div className="forum-section">
        <h3>Memories</h3>
        <p className="loading-state">Loading memories...</p>
      </div>
    );
  }

  return (
    <div className="forum-section">
      <h3>Memories</h3>
      
      {error && <p className="error-message">{error}</p>}
      
      <div className="forum-posts">
        {posts.length === 0 ? (
          <p className="no-posts">No memories shared yet. Be the first to share yours!</p>
        ) : (
          posts.map((post) => (
            <div key={post.id} className="forum-post">
              <div className="post-header">
                <span className="post-username">{post.username || 'Anonymous'}</span>
                <span className="post-date">{formatDate(post.createdAt)}</span>
              </div>
              
              {post.quotedPost && (
                <div className="quoted-post">
                  <div className="quoted-post-header">
                    <span className="quoted-username">{post.quotedPost.username || 'Anonymous'}</span>
                  </div>
                  <div className="quoted-content">{post.quotedPost.content}</div>
                </div>
              )}
              
              <div className="post-content">{post.content}</div>
              
              <div className="post-actions">
                <button 
                  onClick={() => handleQuote(post)}
                  className="btn-quote"
                  disabled={submitting}
                >
                  Quote
                </button>
              </div>
            </div>
          ))
        )}
      </div>

      <form onSubmit={handleSubmit} className="forum-reply-form">
        <h4>{quotedPost ? 'Reply with Quote' : 'Share Your Memory'}</h4>
        
        {quotedPost && (
          <div className="reply-quote-preview">
            <div className="quote-preview-header">
              <span>Replying to {quotedPost.username || 'Anonymous'}</span>
              <button 
                type="button" 
                onClick={handleCancelQuote}
                className="btn-cancel-quote"
              >
                ✕
              </button>
            </div>
            <div className="quote-preview-content">{quotedPost.content}</div>
          </div>
        )}
        
        <textarea
          value={newPostContent}
          onChange={(e) => setNewPostContent(e.target.value)}
          placeholder="Share your memories of this night..."
          className="forum-textarea"
          rows={4}
          disabled={submitting}
          maxLength={2000}
        />
        
        <div className="form-actions">
          {!isAuthenticated ? (
            <button type="button" onClick={() => loginWithRedirect()} className="btn btn-primary">
              Sign in to post
            </button>
          ) : (
            <button 
              type="submit" 
              className="btn btn-primary"
              disabled={!newPostContent.trim() || submitting}
            >
              {submitting ? 'Posting...' : 'Post'}
            </button>
          )}
        </div>
      </form>
    </div>
  );
}
